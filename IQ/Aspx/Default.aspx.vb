Option Infer On
Imports System.Text
Imports dataAccess
Imports System.IO
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Xml.Serialization
Imports System.Data.OleDb

Public Class _Default

    Inherits System.Web.UI.Page

    Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init

    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)
        If Request.QueryString("lid") Is Nothing OrElse iq.SeshAlive(lid) = False OrElse Not AccountHasRight(Request.QueryString("lid"), "DEFPAGE") Then
            If LCase(Request("Key")) <> "m4ster" Then
                Response.Redirect("signin.aspx")
            End If
        End If


        'If Request("Key") <> "M4Ster" Then

        'End If
        If Not clsIQ.IsLoaded Then Exit Sub 'Yield to the masterpage
        ' If iq Is Nothing Then

        '     HttpContext.Current.Session.Timeout = 525600  'The First/Master session (which holds the IQ object) will last for 1 year (which is the maximum permissable)

        'iq = New clsIQ

        'Dim errormessages As List(Of String) = New List(Of String)
        '   Panel1.Controls.Add(iq.load(errormessages))  'This loads the entire object model from the database and returns the status text/timings

        '    Panel1.Controls.Add(OutputErrors(errormessages, 0, True))

        'This is obsolete - we no longer 'self host the service' it's served by/from IIS - see \services\PnA.svc
        'StockWebservice = StartWebservice()  'returns a reference to it (to keep it in scope!)
        'Dim mylit As Literal
        'mylit = New Literal
        'With StockWebservice
        '    mylit.Text = "<p>Stock and price Webservice Started on " & .BaseAddresses(0).AbsoluteUri & " port " & .BaseAddresses(0).Port.ToString & " state is:" & .State.ToString & "</p>"
        'End With
        'Panel1.Controls.Add(mylit)

        '   End If

        If iq.Screens.Count Then
            Dim btnEdit As New Button
            btnEdit.Text = "Edit Channels"
            Dim occ$ = "embed('/editor/editor.aspx?path=Channels&panelID=ctl00_MainContent_EditPanel','ctl00_MainContent_EditPanel',false,false);return false;"
            btnEdit.OnClientClick = occ$
            EditPanel.Controls.Add(btnEdit)
        End If

        LblProducts.Text = iq.Products.Count()

        ' FillTree()
        ' Upd.Update()

        'Dim dt As DataTable = getTranslationGroups()

        'For Each row As DataRow In dt.Rows
        '    Dim lst As ListItem = New ListItem()
        '    If Trim(row(0)) = "" Then
        '        lst.Text = " Blank (" & row(1) & ")"
        '        lst.Value = ""
        '    Else
        '        lst.Text = row(0) & "(" & row(1) & ")"
        '        lst.Value = row(0)
        '    End If

        '    dropGroup.Items.Add(lst)
        'Next
    End Sub


    'Private Sub FillTree()

    '    'Upd.Controls.Clear()
    '    Dim top As New Panel

    '    'To open and close branches without refreshing the page, we need to know which branch was clicked *before* its event fires
    '    'Its even cannot fire unless it has already been added to the control hierarchy - but a branch that is already open and has had its
    '    'collapse' button pressed - we would load the open children (and their descendants)... (and then be left with the problem of removing them)
    '    'by 'pre-emptively' knowing which expand/collapse button was pressed to submit the form - we can toggle that branch whilst filling the tree

    '    'Dim ClickedBranch As String
    '    'ClickedBranch = Request.Form("__EVENTTARGET")
    '    'Dim p() = Split(ClickedBranch, "$")
    '    Dim ToggleBranch As String
    '    ToggleBranch = ""
    '    'If UBound(p) Then
    '    '    ToggleBranch = Replace(p(UBound(p)), "Exp", "")
    '    'End If

    '    Upd.ContentTemplateContainer.Controls.Add(top)
    '    'recursively add all open branches from the root downwards - closing(or opening) togglebranch
    '    AddBranchTotree(iq.Root, top, 1, ToggleBranch, "") ', Upd.Triggers)

    'End Sub

    Private Function Removeitem(ByVal in$, ByVal r$) As String

        'Removes the item R$ from the comma delimited list in$
        'returns the shortened list

        Dim ret$ = ""
        Dim j() As String
        j = Split(in$, ",")

        For Each i In j
            If i <> r$ Then ret$ &= i & ","
        Next

        Return ret

    End Function


    Private Function LastSegment(ByVal Path As String)
        Dim b() As String
        b = Split(Path$, ".")
        LastSegment = b(UBound(b))

    End Function

    'Private Sub FixVariants(HostID As String)

    '    'delete all variants, prices and stock
    '    'for each HostMfrPartnum in h3.iq.products.pricelistmaster - make a new variant

    '    Dim Sql$ = "SELECT MfrPartnum,hostmfrpartnum FROM h3.iq.products.pricelistmaster "
    '    Sql$ &= " WHERE hostID='" & HostID & "' and priceBand is null"

    '    Dim dic As Dictionary(Of String, String) = da.BuildDic(Sql$, "hostmfrpartnum", "mfrpartnum")

    '    Dim channel As clsChannel = iq.i_channel_code(HostID)
    '    Dim v As clsVariant
    '    For Each k In dic.Keys
    '        If iq.i_SKU.ContainsKey(dic(k)) Then
    '            v = New clsVariant("", iq.i_SKU(dic(k)), channel, k, "", "", "", Nothing)
    '        End If
    '    Next

    'End Sub




    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Sub New()

    End Sub

    Protected Sub BtnImport_Click(ByVal sender As Object, ByVal e As EventArgs) Handles BtnImport.Click


        Logit("Starting import", True, False)

        Import.everything()



    End Sub




    '                       MEM            mem-pc2-600         dl360
    ' optTypeCheck(rdr.Item("opttype"), rdr.Item("optfamily"), family) Then

    Public Function OptTypeCheck(category As String, Optfamily As String, Sysfamily As String, dicSysFamDefs As Dictionary(Of String, Dictionary(Of String, String))) As Boolean

        'check that this options optfamily value matches the sysfamilydefinition(category)

        'category is MEM,HDD,OPT,FAN etc.
        'type is mem-pc2-600
        'family is .. the family

        If Optfamily = dicSysFamDefs(Sysfamily)(Optfamily) Then OptTypeCheck = True Else OptTypeCheck = False

    End Function

    Private Sub BtnSerialize_Click(sender As Object, e As System.EventArgs) Handles BtnSerialize.Click

        Dim sw As StreamWriter = New StreamWriter("c:\temp\iq.txt")
        Reflection.Serialize(iq, sw, 1)

        sw.Close()

    End Sub

    Protected Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click


    End Sub



    ''make a product for each type of primary storage (bay eg. 5.25 SFF hot swapppable)

    '    rdr = da.dbexecuteReader(con, "select distinct familypristor from iq.products.sysfamilydefinitions")
    ''Make a product to fit on the branch that will group them
    'Dim objProduct_PriStoreTypes As New clsProduct
    'Dim j As New clsProductAttribute(objProduct_PriStoreTypes, iq.Attributes("Name"), 0, iq.Units("txt"), New clsText(iq.AddText("Primary storage types", s_lang)))
    '    j = Nothing

    '' make the placeholder branch (grouping) branch for all types of primary storage (bay/Form Factor)
    'Dim objBranch_PriStoreTypes As New clsBranch(objProduct_PriStoreTypes, Nothing)

    'Dim atype As clsProduct
    'Dim dicBranches_PriStore As New Dictionary(Of String, clsBranch)

    '    While rdr.Read
    '        If Not IsDBNull(rdr.Item("familyPristor")) Then
    '            atype = New clsProduct(CStr(rdr.Item("Familypristor")), False) 'just making one will add it to the OM's Prodcuts Collection
    '            dicBranches_PriStore.Add(Trim$(rdr.Item("familypristor")), New clsBranch(atype, objBranch_PriStoreTypes))
    '        End If
    '    End While

    '    rdr.Close()

    ''find all the options that are of the known primary storage types  (eg. 5.25 SFF hot swapppable)
    ''.. make a product for each, add it to that PrimaryStorage type branch branch

    '    rdr = da.dbexecuteReader(con, "SELECT optfamily,optSKU,ccdescription FROM iq.products.options_svr AS o JOIN iq.products.hierarchyIQ AS h ON o.optSKU=h.upcnum; ")
    'Dim objProduct_Drive As clsProduct

    'Dim dicBranch_drives As New Dictionary(Of String, clsBranch) 'SKU > branch
    'Dim sku As clsProductAttribute

    'Dim objAttribute_SKU As clsAttribute
    '    objAttribute_SKU = iq.Attributes("MfrSKU")

    'Dim drives As Integer

    '    While rdr.Read

    '        If dicBranches_PriStore.ContainsKey(Trim$(rdr.Item("optfamily"))) Then

    '            objProduct_Drive = New clsProduct 'just making one will add it to the OM's Products Collection
    '            sku = New clsProductAttribute(objProduct_Drive, objAttribute_SKU, 0, iq.Units("txt"), New clsText(iq.AddText(rdr.Item("optsku"), s_lang)))

    'Dim drivedesc As String
    '            drivedesc = rdr.Item("ccdescription")
    '            j = New clsProductAttribute(objProduct_Drive, iq.Attributes("Desc"), 0, iq.Units("txt"), New clsText(iq.AddText(drivedesc, s_lang)))

    '            dicBranch_drives.Add(Trim$(rdr.Item("OptSku")), New clsBranch(objProduct_Drive, dicBranches_PriStore(Trim$(rdr.Item("optfamily")))))

    ''If dicBranches_PriStore(rdr.Item("optfamily")).Branches.Count > 1 Then
    '' Stop
    '' End If

    '            drives += 1
    '        End If
    '    End While

    '    rdr.Close()

    ''now fetch descriptions for all the drives (from ProductHeirachy.. based on the OptSKU's in the drives dictionary

    ''For every System.. graft on (a reference to) the right branch of primary storage based on its family's FamilyPriStor

    '    sql$ = "select d.FamilyPriStor,s.modelSKU from iq.products.systems_svr s join iq.products.sysfamilydefinitions d "
    '    sql$ &= "on s.familycode = d.sysfamily" ' where s.systype='svr'"

    '    rdr = da.dbexecuteReader(con, sql$)

    '    While rdr.Read
    '        If Not IsDBNull(rdr.Item("familypristor")) Then
    '            If dicSystems.ContainsKey(Trim$(rdr.Item("modelsku"))) Then
    ''abranch = New clsBranch(dicBranches_PriStore(rdr.Item("FamilyPriStor")).Product, systems(rdr.Item("modelsku")))
    '                iq.Graft(dicBranches_PriStore(Trim$(rdr.Item("FamilyPriStor"))), dicSystems(Trim$(rdr.Item("modelsku"))))
    '            Else
    '                Stop
    '            End If
    '        End If
    '    End While

    '    rdr.Close()


    'Protected Sub BtnImportSlots_Click(sender As Object, e As EventArgs) Handles BtnImportSlots.Click

    '    'Create an account for techdata
    '    'FROM is a LINQ query

    '    'http://www.experts-exchange.com/Software/Server_Software/Application_Servers/.Net/Q_24706814.html

    '    Dim role As clsRole = (From r In iq.Roles.Values Where r.Code = "admin").First
    '    Dim lang As clsLanguage = (From l In iq.Languages.Values Where l.Code = "EN").First
    '    Dim channel As clsChannel = (From j In iq.Channels.Values Where j.Name.Contains("Computer 2000")).First
    '    Dim currency As clsCurrency = (From c In iq.Currencies.Values Where c.Code = "GBP").First
    '    Dim team As clsTeam = Nothing
    '    Dim User As clsUser = iq.i_user_email("nickax@gmail.com")
    '    Dim password As String = Shuffle(md5("password"))

    '    Beep()
    '    'make the new account
    '    'Dim ac As New clsAccount(User, password, channel, role, team, lang, currency)

    'End Sub


    Protected Sub BtnImportListPrices_Click(sender As Object, e As EventArgs) Handles BtnImportListPrices.Click

        'Imports/updates 

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        Dim l$
        l$ = Import.listprices(con, LPCountry.Text)
        con.Close()
        con.Dispose()

        Form.Controls.Add(NewLit(l$))
        Form.Controls.Add(ErrorDymo(LPCountry.Text & " ONLY!"))
        Form.Controls.Add(NewLit("<p>see c:\temp\import.log for details</p>"))

        'Dim MyConnection As System.Data.OleDb.OleDbConnection
        'Dim DtSet As System.Data.DataSet
        'Dim MyCommand As System.Data.OleDb.OleDbDataAdapter
        'MyConnection = New System.Data.OleDb.OleDbConnection("provider=Microsoft.Jet.OLEDB.4.0;Data Source='c:\temp\hplistprices.xlsx';Extended Properties=Excel 8.0;")

        'MyConnection.Open()

        'MyCommand = New System.Data.OleDb.OleDbDataAdapter("select * from [HPSD$]", MyConnection)
        '    'MyCommand.TableMappings.Add("Table", "Net-informations.com")
        '    DtSet = New System.Data.DataSet
        'Try
        '    MyCommand.SelectCommand.Connection = MyConnection
        '    MyCommand.Fill(DtSet)
        'Catch ex As System.Exception

        '    Beep()

        'End Try

        '    Dim sheetreader As System.Data.DataTableReader
        '    sheetreader = DtSet.CreateDataReader()

        'Dim ob() As Object = Nothing
        'ReDim ob(35)

        'sheetreader.Read() 'Countries (from column 9 - FR)

        'sheetreader.Read() 'currencies

        ''Conversions for HP's dodgy internal currency codes
        'Dim dichpcurr As Dictionary(Of String, String)
        'dichpcurr = New Dictionary(Of String, String)
        'dichpcurr.Add("EC", "EUR")
        'dicHpcurr.add("BP", "GBP")
        'dicHpcurr.add("SK", "SEK")
        'dicHpcurr.add("NK", "NOK")
        'dicHpcurr.add("DK", "DKK")
        'dicHpcurr.add("SF", "SFR")
        'dicHpcurr.add("PZ", "PLN")
        'dicHpcurr.add("CK", "CZK")
        ''dicHpcurr.add "HF","CZK"  'Hungarian forint - not implimented
        'dicHpcurr.add("RR", "RUB")
        'dicHpcurr.add("RD", "SAR") 'rand

        'sheetreader.Read() 'Countries (from column 9 - FR)
        'sheetreader.Read() 'Countries (from column 9 - FR)
        'sheetreader.Read() 'Countries (from column 9 - FR)


        '    Do
        '        While sheetreader.Read
        '        sheetreader.GetValues(ob)

        '            For i = 0 To UBound(ob)
        '                Debug.Print(sheetreader.GetName(i) & ob(i))
        '            Next

        '        End While

        '    Loop While sheetreader.NextResult() 'move to the next result set (sheet)

        '    sheetreader.Close()
        '    MyCommand = Nothing


        'MyConnection.Close()

    End Sub

    Protected Sub Button2_Click(sender As Object, e As EventArgs) Handles BtnIndexProducts.Click

        Dim count As Integer
        count = IndexPaths()
        Response.Write("Wrote " & count & " [Path] rows")

    End Sub


    Protected Sub BtnMakeScreens_Click(sender As Object, e As EventArgs) Handles BtnMakeScreens.Click
        PruneOffNonCompatableFamilyMinorSLotTypes()
        'Import.EnableSASLicense()

        'Import.Incremental(New List(Of String) From {"726660-B21"})

        'Dim errormessages As List(Of String) = New List(Of String)

        'da.DBExecutesql("DELETE FROM [field]")
        'da.DBExecutesql("DELETE FROM [InputType]")
        'da.DBExecutesql("DELETE FROM Screen")

        'Dim atype As clsInputType
        'iq.InputTypes.Clear()
        'iq.i_inputType_code.Clear()

        'atype = New clsInputType("single", "Single")
        'atype = New clsInputType("string", "Text")
        'atype = New clsInputType("int32", "Integer")
        'atype = New clsInputType("date", "Date")
        'atype = New clsInputType("many", "Many")  'dictionaries of other (editable) things
        'atype = New clsInputType("one", "One")
        'atype = New clsInputType("boolean", "Boolean")
        'atype = New clsInputType("translate", "Translation")
        'atype = New clsInputType("nullstring", "Nullable String")
        'atype = New clsInputType("nullint", "Nullable Integer")
        'atype = New clsInputType("customerprice", "customer specific price")
        'atype = New clsInputType("nullprice", "nullable price")
        'atype = New clsInputType("icon", "icon")  'an icon is a special field renders a productattribute - It's shown (as an icon) against a product if it's present - its (numeric) value becomes a tooltip - it *should not* have a translation
        'atype = New clsInputType("xNote", "xNote")  'a note is a special field - it's a product attribute with an image based ont he numeric value, - It also has a translation and it's visibility can be controlled by the HideF and ShowF (show and hide under family) attributes

        'iq.Screens.Clear()
        'iq.i_screens_title.Clear()
        'iq.Fields.Clear()

        ''make the screen for managing screens
        'Dim screen As clsScreen
        'screen = MakeScreen("Screen", "scrn", GetType(clsScreen), Nothing, errormessages)

        ''     screen = MakeScreen("Channel", GetType(clsChannel), Nothing)
        '' screen = MakeScreen("Product", GetType(clsProduct), Nothing)
        '' screen = MakeScreen("Thread", GetType(clsThread), Nothing)
        '' screen = MakeScreen("State", GetType(clsState), Nothing)


        ''load up what we just created
        'Dim con As SqlClient.SqlConnection
        'Dim rdr As SqlClient.SqlDataReader = Nothing
        'con = da.OpenDatabase()
        'iq.LoadScreens(con, rdr)
        'con.Close()
        'con.Dispose()

        'Response.Redirect("default.aspx") ' refresh the page to remake the edit button 


    End Sub

    Protected Sub BtnImportUsers_Click(sender As Object, e As EventArgs) Handles BtnImportUsers.Click

        iq.Users.Clear()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        da.DBExecutesql(con, "delete from [user]")

        'USERS, ACCOUNTS and TEAMS about 21 seconds
        Dim dicChannels As Dictionary(Of String, clsChannel) = loadDic(con, iq.Channels, "channel")
        Dim dicAccounts As Dictionary(Of String, clsAccount) = loadDic(con, iq.Accounts, "account")
        Dim dicTeams As Dictionary(Of String, clsTeam) = loadDic(con, iq.Teams, "team")
        Dim dicUsers As Dictionary(Of String, clsUser) ' = loadDic(con, iq.Users, "user", EventDicLoad)

        dicUsers = New Dictionary(Of String, clsUser)
        dicAccounts = New Dictionary(Of String, clsAccount)

        Import.users(con, dicChannels, dicAccounts, dicTeams, dicUsers)

        saveDic(con, dicAccounts, "account")
        saveDic(con, dicTeams, "team")
        saveDic(con, dicUsers, "user")

    End Sub

    Protected Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click


        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        Import.Regions(con)
        con.Close()
        con.Dispose()


    End Sub

    'OBSOLETED by variants

    'Protected Sub BtnImportChannelSKUs_Click(sender As Object, e As EventArgs) Handles BtnImportChannelSKUs.Click

    '    Dim con As SqlClient.SqlConnection
    '    con = da.opendatabase()

    '    da.dbexecutesql("DELETE FROM CHANNELSKU")


    '    Dim dt As DataTable = da.MakeWriteCacheFor(con, "ChannelSKU")

    '    Dim sql$
    '    sql$ = "SELECT distinct HOSTID,hostpartnum,hostmfrpartnum from iq.products.pricelistmaster Where hostpartnum is not null or hostmfrpartnum is not null"

    '    Dim rdr As SqlClient.SqlDataReader
    '    Dim row As System.Data.DataRow

    '    Dim rows As Integer = 0
    '    Dim failed As Integer = 0

    '    Dim hmpn As String

    '    rdr = da.dbexecuteReader(con, sql$)
    '    While rdr.Read

    '        If Not IsDBNull(rdr.Item("hostmfrpartnum")) Then
    '            hmpn = Split(rdr.Item("hostmfrpartnum"), "#")(0)

    '            If iq.i_SKU.ContainsKey(hmpn) Then
    '                If iq.i_channel_code.ContainsKey(rdr.Item("HOSTID")) Then
    '                    row = dt.NewRow()
    '                    row("fk_channel_id") = iq.i_channel_code(rdr.Item("hostid")).ID
    '                    row("fk_product_id") = iq.i_SKU(hmpn).ID
    '                    row("fk_variant_id") = iq.StandardVariant.ID

    '                    If IsDBNull(rdr.Item("HostPartnum")) Then   'westcoast (for example) don't provide a host partnum
    '                        row("channelSKU") = rdr.Item("hostmfrpartnum") 'use thier HostMfrParNum (complete with any #code)
    '                    Else
    '                        row("channelSKU") = rdr.Item("hostpartnum")
    '                    End If
    '                    row("channelMfrSKU") = rdr.Item("HostMfrPartNum")

    '                    dt.Rows.Add(row)
    '                    rows += 1
    '                Else

    '                    failed += 1
    '                End If

    '            Else
    '                failed += 1
    '            End If
    '        Else
    '            failed += 1
    '        End If

    '    End While
    '    rdr.Close()

    '    da.bulkwrite(con, dt, "ChannelSKU")

    '    con.Close()
    '    con.Dispose()

    '    Response.Write("Imported " & rows)

    'End Sub

    Protected Sub BtnImportFormFactors_Click(sender As Object, e As EventArgs) Handles BtnImportFormFactors.Click

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()

        Dim dicFF As Dictionary(Of String, clsTranslation)
        dicFF = Import.FormFactors(con)

        Dim rdr As SqlClient.SqlDataReader

        Dim writecache As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")

        Dim sql$

        sql$ = "delete from translation where [group]='ff'"
        iq.LoadTranslations(con)


        sql$ = "Delete from iquote2.productAttribute where fk_attribute_id=" & iq.i_attribute_code("formFactor").ID
        'sql$ = "select modelsku,sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily join iq.dbo.abbreviations a on a.code=ff"
        sql$ = "select modelsku,a.Translation,sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily join iq.dbo.abbreviations a on a.code=sf.InstFormFactor"
        rdr = da.DBExecuteReader(con, sql$)


        Dim system As clsProduct
        Dim formfactor As clsProductAttribute

        Dim count As Integer = 0
        Dim rows As Integer = 0

        Dim tl As clsTranslation
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("MODELSKU")) Then
                system = iq.i_SKU(rdr.Item("modelsku"))
                If system.isSystem Then
                    If dicFF.ContainsKey(rdr.Item("translation")) Then
                        tl = dicFF(rdr.Item("translation"))
                        formfactor = New clsProductAttribute(system, iq.i_attribute_code("formFactor"), 0, iq.i_unit_code("txt"), tl, writecache)
                        count += 1
                    Else
                        Beep()
                    End If
                End If
                rows += 1
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, writecache, "ProductAttribute")

        con.Close()
        con.Dispose()

    End Sub
    Private Function alphaval(l$) As Integer

        'return a numeric value from the first 4 chars of string

        alphaval = 0

        Dim p As Integer = 1
        For i As Integer = 1 To 4
            alphaval += Asc(Mid(l$, i, 1))
            p = p * 256
        Next

    End Function

    Protected Sub btnAvalanche_Click(sender As Object, e As EventArgs) Handles btnAvalanche.Click

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Import.RefCodes(con)
        Import.Avalanche(con)

        con.Close()
        con.Dispose()

    End Sub

    Protected Sub BtnImportBundles_Click(sender As Object, e As EventArgs) Handles BtnImportBundles.Click
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        ' Import.Bundles(con)
        con.Close()
        con.Dispose()

    End Sub

    Protected Sub SetScreen_Click(sender As Object, e As EventArgs) Handles SetScreen.Click

        Dim hdds As clsScreen = (From r In iq.Screens.Values Where r.title = "HDDs").First
        Dim mem As clsScreen = (From r In iq.Screens.Values Where r.title = "Memory").First
        Dim laptops As clsScreen = (From r In iq.Screens.Values Where r.title = "Laptops").First
        Dim desktops As clsScreen = (From r In iq.Screens.Values Where r.title = "Desktops").First
        Dim servers As clsScreen = (From r In iq.Screens.Values Where r.title = "Servers").First
        Dim carePacks As clsScreen = (From r In iq.Screens.Values Where r.title = "CarePack").First

        If hdds Is Nothing Then Stop
        If mem Is Nothing Then Stop

        Dim errormessages As List(Of String) = New List(Of String)
        iq.RootBranch.Matrix = servers
        iq.RootBranch.Update(errormessages)

        For Each branch In iq.Branches.Values
            Dim bn$ = LCase(branch.Translation.text(English))

            If InStr(bn, "hard disk") Or LCase(branch.Translation.text(English)) = "storage" Then
                branch.Matrix = hdds
                branch.Update(errormessages)
            ElseIf bn = "memory" Then  'strictly equal !
                branch.Matrix = mem
                branch.Update(errormessages)
            ElseIf bn = "laptops" Then  'strictly equal !
                branch.Matrix = laptops
                branch.Update(errormessages)
            ElseIf bn = "servers" Then  'strictly equal !
                branch.Matrix = servers
                branch.Update(errormessages)
            ElseIf bn = "desktops" Then  'strictly equal !
                branch.Matrix = desktops
                branch.Update(errormessages)
            ElseIf bn = "hp networking" Then  'strictly equal !
                branch.Matrix = servers
                branch.Update(errormessages)
            ElseIf bn = "hp storage" Then  'strictly equal !
                branch.Matrix = servers
                branch.Update(errormessages)

            ElseIf branch.Product Is Nothing Then  ' a placeholder branch
                ' If branch.Product.isSystem Then
                If Not branch Is iq.RootBranch Then
                    Select Case bn
                        Case "smart buy", "regular models", "top value"

                        Case "cpus"

                        Case "warranty", "services"
                            branch.Matrix = carePacks
                            branch.Update(errormessages)

                        Case Else
                            branch.Matrix = Nothing 'iq.Screens(719)
                            branch.Update(errormessages)

                    End Select
                End If
            End If

            '            If LCase(branch.Translation.text(English)) = "performance" Then branch.Picture = "tab" : branch.Update()

        Next

    End Sub

    'Protected Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

    '    Response.Write(Import.HostPrices(TxtHostID.Text, "325009"))



    'End Sub

    Protected Sub PtnPower_Click(sender As Object, e As EventArgs) Handles PtnPower.Click

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        Dim l$
        l$ = Import.PowerSizing(con)
        con.Close()
        con.Dispose()

        Response.Clear()
        Response.Write("<p>" & l$ & "</p>")

    End Sub

    Protected Sub BtnExtText_Click(sender As Object, e As EventArgs) Handles BtnExtText.Click

        Stop
        'Response.Write(Import.ExtText())

    End Sub

    Protected Sub btnSlotAdds_Click(sender As Object, e As EventArgs) Handles btnSlotAdds.Click

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Response.Write(Import.slotAdds(con))
        con.Close()
        con.Dispose()

    End Sub


    Protected Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click

        Dim chassisvariant As clsVariant


        Dim chassis = From j In iq.Products.Values Where j.ProductType Is iq.i_ProductType_Code("CHAS")
        For Each cp In chassis
            chassisvariant = New clsVariant("", cp, HP, "", "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
        Next cp

    End Sub

    Protected Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click

        ' FixVariants("DAZRG248NE")

    End Sub

    Protected Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click


        Dim con As SqlClient.SqlConnection = da.OpenDatabase()


        Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")
        Dim dicRegions As Dictionary(Of String, clsRegion) = loadDic(con, iq.Regions, "region")


        'around 18 seconds
        Dim dicAutoAdds As Dictionary(Of String, clsProduct)
        dicAutoAdds = loadDic(con, iq.Products, "autoAdd")

        dicAutoAdds = New Dictionary(Of String, clsProduct) 'remove this to to a delta import

        Response.Write(Import.autoadds(con, dicAutoAdds, dicSystems, dicRegions))
        Response.Write("see c:\temp\import.log  and c:\temp\autoadds.txt")
        saveDic(con, dicAutoAdds, "autoadd")


        con.Close()



    End Sub

    Protected Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click

        'order branches

        Dim errormessages As List(Of String) = New List(Of String)

        Dim otr As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        Dim otp As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)

        Dim sql$
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim rdr As SqlClient.SqlDataReader

        sql$ = "SELECT optTypeCode,optTypeName,optTypeRank FROM iq.products.optTypes"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If rdr.Item("optTypeRank") Is DBNull.Value Then
                otr.Add(rdr.Item("optTypeName"), 100)
            Else
                If Not otr.ContainsKey(rdr.Item("optTypeName")) Then
                    otr.Add(rdr.Item("optTypeName"), rdr.Item("optTypeRank"))
                End If
            End If
        End While
        rdr.Close()

        sql$ = "SELECT optTypeParent,parentRank FROM iq.products.optTypeParents"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If rdr.Item("parentRank") Is DBNull.Value Then
                otp.Add(rdr.Item("optTypeParent"), 100)
            Else
                otp.Add(rdr.Item("optTypeParent"), rdr.Item("parentRank"))
            End If
        End While

        rdr.Close()
        con.Close()

        For Each branch In iq.Branches.Values
            If branch.Product Is Nothing Then
                If branch.Parent Is Nothing Then 'it's a category
                    If otp.ContainsKey(branch.EnglishName) Then
                        branch.order = otp(branch.EnglishName)
                        branch.Update(errormessages)
                    End If
                Else
                    If otr.ContainsKey(branch.EnglishName) Then
                        branch.order = otr(branch.EnglishName)
                        branch.Update(errormessages)
                    End If
                End If
            End If


        Next

    End Sub

    Protected Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click

        Import.CarePackProperties()

    End Sub

    Protected Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click

        'Import loyalty points

        Import.LoyaltyPoints()

    End Sub

    Protected Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click


        Dim errormessages As List(Of String) = New List(Of String)
        Import.Receta(errormessages)
        OutputErrors(Form.Controls, errormessages, 0, True)

    End Sub

    Protected Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click

        Import.FlexOPGs()

    End Sub

    Protected Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click

        Dim errorMessages As List(Of String) = New List(Of String)
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Logit("cpuImport", True)
        Import.CPUs(con, errorMessages)
        con.Close()
        OutputErrors(Form.Controls, errorMessages, 0, True)

    End Sub

    Protected Sub Button2_Click1(sender As Object, e As EventArgs) Handles Button2.Click


        Dim errormessages As List(Of String) = New List(Of String)

        For Each branch In iq.Branches.Values
            If InStr(LCase(branch.Picture), "/iq/images") > 0 Then
                branch.Picture = Replace(branch.Picture, "/iq/images", "/images/iq")
                branch.Update(errormessages)
            End If
        Next

    End Sub

    Protected Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        Import.TopRecommendations()
    End Sub

    Protected Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        Import.HighPerformance()
    End Sub

    Protected Sub Button17_Click(sender As Object, e As EventArgs) Handles Button17.Click
        Import.energyStar()
    End Sub


    Protected Sub Button18_Click(sender As Object, e As EventArgs) Handles Button18.Click

        'read through all the source code . .
        'get (in advance of it every displaying) a copy of every 'key'

        Dim sw As New StreamWriter("C:\Download\xlt\xlt.txt")

        Dim sqlString As String = "SELECT *  FROM [WWW3.CHANNELCENTRAL.NET\CHARLIE,8484].[iQ].[dbo].[Language_Store] ls1 "
        Dim rdr As SqlClient.SqlDataReader
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        rdr = da.DBExecuteReader(con, sqlString)

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)
        For Each Folder In My.Computer.FileSystem.GetDirectories("c:\sites\iq\iq")
            For Each File In My.Computer.FileSystem.GetFiles(Folder)

                Dim sr As New StreamReader(File)
                Dim l() As String = sr.ReadToEnd.Split(vbCrLf)
                sr.Close()

                For Each ln In l
                    If ln.ToLower.Contains("xlt(") Then
                        Dim txtpos As Integer = InStr(ln.ToLower(), "xlt(")
                        Dim txtpos1 As Integer = InStr(txtpos + 5, ln, Chr(34), CompareMethod.Text)
                        If txtpos1 > txtpos Then
                            ' sw.WriteLine(Mid(ln, txtpos + 5, txtpos1 - (txtpos + 5)))
                            'Dim txtpos2 As Integer = InStr(txtpos1 + 1, ln, Chr(34))
                            'If txtpos2 > txtpos1 Then
                            '    sw.WriteLine(Mid(ln, txtpos1, txtpos2 - txtpos1))
                            'End If

                            AddNewTranslation(Mid(ln, txtpos + 5, txtpos1 - (txtpos + 5)), sw, dt)

                        End If
                        ' sw.WriteLine(ln)  'needs some more parseing
                    End If
                    If ln.ToLower.Contains("<asp:") Then
                        If ln.ToLower.Contains(" text") Then
                            Dim txtpos As Integer = InStr(ln.ToLower(), " text")
                            Dim txtpos1 As Integer = InStr(txtpos + 1, ln, Chr(34), CompareMethod.Text)
                            If txtpos1 > txtpos Then
                                Dim txtpos2 As Integer = InStr(txtpos1 + 1, ln, Chr(34))
                                If txtpos2 > txtpos1 Then
                                    ' sw.WriteLine(Mid(ln, txtpos1 + 1, txtpos2 - (txtpos1 + 1)))

                                    AddNewTranslation(Mid(ln, txtpos1 + 1, txtpos2 - (txtpos1 + 1)), sw, dt)
                                End If
                            End If
                        ElseIf ln.ToLower.Contains("<asp:label") Then
                            Dim txtpos As Integer = InStr(ln, ">")
                            Dim txtpos1 As Integer = InStr(txtpos + 1, ln, "<", CompareMethod.Text)
                            If txtpos1 > txtpos Then
                                ' sw.WriteLine(Mid(ln, txtpos + 1, txtpos1 - (txtpos + 1)))
                                Dim t$ = Mid(ln, txtpos + 1, txtpos1 - (txtpos + 1))
                                AddNewTranslation(t$, sw, dt)
                            End If
                            '  sw.WriteLine(ln)
                        End If
                    End If
                    'same for any .caption = "
                    'or .text =
                    '(etc... see The form controls translations in Masterpage)
                Next
            Next
        Next

        sw.Close()

        'Make another streamreader here - and read back all the translations - making a KY one, and a EN one
        'If you create them all with a GROUP of 'UI' - we can delete them all together (for testing/refreshing)
        'we can also make french german spanish etc - for anything we already have an exact translation for in IQ1

    End Sub

    Private Sub AddNewTranslation(p1 As String, ByRef sw As StreamWriter, ByRef dt As DataTable)
        Dim importTranslation As clsTranslation
        Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First
        If p1.Contains("'") Then
            p1 = p1.Replace("'", "~")
            p1 = p1.Replace("~", "''")
        End If

        Dim queryString As String = "Translation = '" & p1 & "'"
        Dim otherLanguage As clsLanguage
        Dim result As DataRow() = dt.Select(queryString)

        Dim rowLanguage As String

        If result.Length > 0 Then
            Dim item As DataRow = result(0)
            If Not iq.KYIndex.ContainsKey(p1) Then
                importTranslation = New clsTranslation(kyLanguage, p1)

                queryString = "textID =" & item("textID").ToString()
                Dim rows As DataRow() = dt.Select(queryString)
                For Each row As DataRow In rows
                    rowLanguage = row("lang")
                    otherLanguage = iq.i_language_Code(rowLanguage.ToUpper) ' (From l In iq.Languages.Values Where l.Code = rowLanguage.ToUpper).First
                    importTranslation.addLanguage(otherLanguage, row("Translation"), dt)
                    importTranslation.Update(otherLanguage)
                Next
            End If
        Else
            sw.WriteLine(p1)

        End If

    End Sub

    Protected Sub Button19_Click(sender As Object, e As EventArgs) Handles Button19.Click

        Import.fixProductFamilies()


    End Sub

    Protected Sub Button20_Click(sender As Object, e As EventArgs) Handles Button20.Click
        Import.defaultWarranty()

    End Sub


    Protected Sub Button22_Click(sender As Object, e As EventArgs) Handles Button22.Click

        Dim lit As Literal = New Literal

        Dim errormessages As List(Of String) = New List(Of String)
        lit.Text = "<p>" & GetVariants(txtHostCode.Text, "", errormessages) & "</p>"
        Form.Controls.Add(lit)


    End Sub

    Protected Sub Button23_Click(sender As Object, e As EventArgs) Handles Button23.Click


        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Import.quoteStates(con)
        con.Close()


    End Sub

    Public Sub Button24_Click(sender As Object, e As EventArgs) Handles Button24.Click

        Dim errormessages As List(Of String) = New List(Of String)

        SetRCAs(errormessages)

        OutputErrors(Form.Controls, errormessages, 0, True)
        Form.Controls.Add(NewLit("Done"))

    End Sub

    Protected Sub btnSBSO_Click(sender As Object, e As EventArgs) Handles btnSBSO.Click
        Page.Controls.Add(NewLit("<p>" & Import.Hierarchy() & "</p>"))
        '  Dim output As String = IQDrive.uploadFile()
    End Sub

    Shared Sub SetRCAs(ByRef errorMessages As List(Of String))

        Dim how As Dictionary(Of String, List(Of Integer)) = New Dictionary(Of String, List(Of Integer))
        iq.RootBranch.setRCA(1, iq.RootBranch, how, errorMessages)

        For Each h In how.Keys

            Dim done As Integer = 0
            Dim alldone As Boolean = False
            Do
                Dim lump = (From v In how(h) Select (CStr(v))).Skip(done).Take(5000)
                If lump.Count < 5000 Then alldone = True
                Dim ids() As String = lump.ToArray
                da.DBExecutesql("UPDATE branch set rca ='" & h & "' WHERE ID IN (" & Join(ids, ",") & ");")
                done += 5000
            Loop Until alldone

        Next

    End Sub

    Protected Sub Button25_Click(sender As Object, e As EventArgs) Handles Button25.Click

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Page.Controls.Add(NewLit(quoteImport.all(con)))
        con.Close()

    End Sub

    Protected Sub btnPreInstalled_Click(sender As Object, e As EventArgs) Handles btnPreInstalled.Click
        Import.preInstalledParts()

    End Sub



    Protected Sub btnImpcrePacks_Click(sender As Object, e As EventArgs) Handles btnImpcrePacks.Click
        'Import.CarePacks()
        Import.MLCarePacks()
    End Sub

    Protected Sub cmdQuickSpecs_Click(sender As Object, e As EventArgs) Handles cmdQuickSpecs.Click
        Import.ImportQuickSpecs()
    End Sub


    Protected Sub cmdExtras_Click(sender As Object, e As EventArgs) Handles cmdExtras.Click
        Import.Extras()
    End Sub

    Protected Sub btnGraphics_Click(sender As Object, e As EventArgs) Handles btnGraphics.Click
        Import.Graphics()
    End Sub

    Protected Sub cmdNetworking_Click(sender As Object, e As EventArgs) Handles cmdNetworking.Click
        Import.Networking()
    End Sub

    Protected Sub cmdPSU_Click(sender As Object, e As EventArgs) Handles cmdPSU.Click
        Import.ImportPSU()
    End Sub

    Protected Sub cmdOS_Click(sender As Object, e As EventArgs) Handles cmdOS.Click
        Import.OSs()
    End Sub

    Protected Sub btnGenerateLang_Click(sender As Object, e As EventArgs) Handles btnGenerateLang.Click
        'Use this set to export KY index
        'Using writer As StreamWriter = New StreamWriter("C:\translationexport.txt")

        '    writer.WriteLine("KY|EN" & IIf(txtLang.Text <> "EN", "|" & txtLang.Text, ""))


        '    Dim list = From j In iq.Translations.Values Where j.Group <> ""
        '    For Each translation As clsTranslation In list
        '        Dim outputString As String
        '        outputString = translation.textTranslation(iq.i_language_Code("KY")) & "|" & translation.textTranslation(iq.i_language_Code("EN")) & IIf(txtLang.Text <> "EN", "|" & translation.textTranslation(iq.i_language_Code(txtLang.Text)), "")

        '        writer.WriteLine(outputString)
        '    Next
        'End Using


        'Use this to export all 
        Using writer As StreamWriter = New StreamWriter("C:\translationexport.txt")

            writer.WriteLine("EN|KeyCode|Group" & IIf(txtLang.Text <> "EN", "|" & txtLang.Text, ""))


            Dim list = From j In iq.Translations.Values
            For Each translation As clsTranslation In list
                Dim outputString As String
                outputString = Replace(translation.textTranslation(iq.i_language_Code("EN")), vbTab, "") & "|" & translation.Key & "|" & Replace(translation.Group, vbTab, "") & IIf(txtLang.Text <> "EN", "|" & translation.textTranslation(iq.i_language_Code(txtLang.Text)), "")

                writer.WriteLine(outputString)
            Next
        End Using




    End Sub
    Protected Sub btnImportTranslation_Click(sender As Object, e As EventArgs) Handles btnImportTranslation.Click

        'File format will be | deliminated column headers to be language id or language name, left column is ALWAYS the key used
        Dim savePath As String = Path.GetTempPath()
        Dim line As String = Nothing
        Dim firstrow As Boolean = False
        Dim itemColumns As Integer = 0
        Dim transLang As clsLanguage = Nothing
        If uploadTranslations.HasFile Then
            savePath += uploadTranslations.FileName
            uploadTranslations.SaveAs(savePath)

            Dim lines = File.ReadAllLines(savePath, Encoding.Unicode)
            Dim CSVLanguages As List(Of clsLanguage) = New List(Of clsLanguage)()
            Dim sourceLanguage = New Dictionary(Of String, List(Of clsTranslation))()

            Dim errors As List(Of String) = New List(Of String)()
            Dim haveHeaders As Boolean = False
            For Each line In lines
                Dim columns = line.Split("|")
                If Not haveHeaders Then
                    'Do we have enough
                    If columns.Count < 2 Then Throw New Exception("Not enough columns detected")

                    For Each col In columns
                        Dim l = findLanguage(col)
                        If CSVLanguages.Count = 0 AndAlso Not {"KY", "EN"}.Contains(l.Code) Then Throw New Exception("Can only convert KY or English for now!")
                        If CSVLanguages.Count = 0 Then
                            sourceLanguage = iq.Translations.GroupBy(Function(ei) ei.Value.text(l).ToLower).ToDictionary(Function(ei) ei.Key, Function(ei) ei.Select(Function(ei2) ei2.Value).ToList())
                        End If
                        If l Is Nothing Then Throw New Exception("Cannot convert language")
                        CSVLanguages.Add(l)
                    Next
                    haveHeaders = True
                Else
                    'Normal data line - for now we can only deal with English, ID or KY as the key
                    Select Case CSVLanguages(0).Code
                        Case Is = "EN"
                            If sourceLanguage.ContainsKey(columns(0).ToString.ToLower) Then
                                For Each t In sourceLanguage(columns(0).ToString.ToLower)
                                    t.addLanguage(CSVLanguages(1), columns(1), Nothing)
                                Next
                            Else
                                errors.Add("Could not find english for:" & columns(0))
                            End If
                        Case Is = "KY"
                            If iq.KYIndex.ContainsKey(columns(0)) Then
                                iq.KYIndex(columns(0).ToString).addLanguage(CSVLanguages(1), columns(1), Nothing)
                            Else
                                errors.Add("Could not find english for:" & columns(0))
                            End If
                    End Select
                End If

            Next


            'Using reader As StreamReader = New StreamReader(savePath)
            '    line = reader.ReadLine
            '    Do While line IsNot Nothing
            '        Dim stringItems() As String = Split(line, "|")
            '        If firstrow = False Then
            '            itemColumns = stringItems.Count
            '            If itemColumns > 3 Then
            '                transLang = iq.i_language_Code(stringItems(3))
            '            End If
            '            firstrow = True
            '        Else
            '            If stringItems.Count > 0 And transLang IsNot Nothing And IsNumeric(stringItems(1)) Then

            '                Dim trans As clsTranslation = iq.Translations(CInt(stringItems(1)))
            '                trans.addLanguage(transLang, stringItems(3), Nothing)
            '            End If
            '        End If
            '        line = reader.ReadLine
            '    Loop
            'End Using
            For Each er In errors
                AuditLog.Instance.Add(AuditType.Warning, er, "ImportTran", 0)
            Next

        End If
    End Sub
    Private Function findLanguage(lk As Object) As clsLanguage
        findLanguage = Nothing
        Dim id2 As Integer
        'do we recognise the language?
        If Integer.TryParse(lk.ToString, id2) AndAlso iq.Languages.ContainsKey(CInt(lk)) Then
            findLanguage = iq.Languages(CInt(lk))
        Else
            If iq.i_language_Code.ContainsKey(lk) Then findLanguage = iq.i_language_Code(lk)
        End If


    End Function
    Private Sub ProcessTranslations(file As HttpPostedFile)
        Dim myStream As System.IO.Stream
        Dim fileLen As Int32
        Dim displayString As StringBuilder = New StringBuilder()

        ' Get the length of the file.
        fileLen = uploadTranslations.PostedFile.ContentLength

        ' Create a byte array to hold the contents of the file.
        Dim Input(fileLen) As Byte
        ' Initialize the stream to read the uploaded file.
        myStream = uploadTranslations.FileContent

        ' Read the file into the byte array.
        myStream.Read(Input, 0, fileLen)

        'Copy the byte array to a string

        For loop1 As Integer = 0 To loop1 < fileLen
            displayString.Append(Input(loop1).ToString())
        Next


    End Sub

    Protected Sub Button26_Click(sender As Object, e As EventArgs) Handles Button26.Click

        'standalone new options import

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim dicfamilies As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "family")

        Logit("building PLcode lookup dictionary")
        Dim dicplcode As Dictionary(Of String, String)
        dicplcode = LoadPLCodes(con) 'generates a dictionary of SKU>Plcode

        Dim dicUnits As Dictionary(Of String, clsUnit) = New Dictionary(Of String, clsUnit)  ' = loadDic(con, iq.Units, "unit", AnEvent)
        Import.units(con, dicUnits)
        '        saveDic(con, dicUnits, "unit", anEvent)

        Dim dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)) = New Dictionary(Of clsProduct, List(Of clsRegion))
        Dim opts As Dictionary(Of String, clsBranch) = Import.options2(con, dicplcode, dicUnits, dicOptLocalisation, clsRegion.containment)

        Response.Write("Loaded " & opts.Count & " options")
        Response.Write("Options root is" & opts("ALL").ID)


        con.Close()
        con = da.OpenDatabase

        Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")

        con = da.OpenDatabase()
        Import.Buildtree2(con, opts, dicfamilies, dicSystems, dicOptLocalisation)

        con.Close()

    End Sub


    Protected Sub Button27_Click(sender As Object, e As EventArgs) Handles Button27.Click


        Dim errormessages As New List(Of String)
        iq.RootBranch.OrderFamilies(1, errormessages)


    End Sub

    Protected Sub Button28_Click(sender As Object, e As EventArgs) Handles Button28.Click

        'prunes off options which are not listed as comatible with their family

        Import.DoPrunes()


    End Sub



    Protected Sub Button29_Click(sender As Object, e As EventArgs)
        SaveUserStates()
    End Sub

    Protected Sub Button30_Click(sender As Object, e As EventArgs) Handles Button30.Click
        Import.SoftwareSlots()
    End Sub

    Protected Sub Button31_Click(sender As Object, e As EventArgs) Handles Button31.Click
        Import.chassisMemSlots()
    End Sub

    Protected Sub Button32_Click(sender As Object, e As EventArgs) Handles Button32.Click
        Import.DoPrunes()
    End Sub

    Protected Sub Button33_Click(sender As Object, e As EventArgs) Handles Button33.Click
        Import.fixFamMinor()

    End Sub


    Protected Sub Button34_Click(sender As Object, e As EventArgs) Handles Button34.Click
        Import.fixPci()

    End Sub

    Protected Sub ButtonOP_Click(sender As Object, e As EventArgs)
        Import.InterfaceSlots()
    End Sub

    Protected Sub Button35_Click(sender As Object, e As EventArgs) Handles Button35.Click


        Dim avariant As clsVariant
        Dim aprice As clsPrice

        Dim vwc As DataTable
        Dim pwc As DataTable

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase

        Dim chn As clsChannel = iq.i_channel_code("ACHCM72YN")

        da.DBExecutesql("delete from price where fk_variant_id in (select Id from variant where fk_channel_id_seller= " & chn.ID & ")")
        da.DBExecutesql("delete from stock where fk_variant_id in (select Id from variant where fk_channel_id_seller= " & chn.ID & ")")
        da.DBExecutesql("delete from variant where fk_channel_id_seller= " & chn.ID)

        Dim nvid As Integer
        Dim npid As Integer
        vwc = da.MakeWriteCacheFor(con, "variant", nvid, True)
        pwc = da.MakeWriteCacheFor(con, "price", npid, True)

        chn.priceConfig = 5  'Take off the 'webservice' Bit
        Dim errormessages As List(Of String) = New List(Of String)
        chn.Update(errormessages)

        For Each sku In iq.i_SKU.Keys
            Dim product As clsProduct = iq.i_SKU(sku)
            avariant = New clsVariant("AAV", product, chn, "AA-" & sku, "", "", "", r_worldwide, 0, vwc, nvid)
            '  aprice = New clsPrice(avariant, iq.getPriceBand(""), New NullablePrice(CSng(1), iq.i_currency_code("GBP"), False), "FAKE", pwc, npid)
        Next

        da.BulkWrite(con, vwc, "Variant", , True)
        da.BulkWrite(con, pwc, "Price", , True)

        con.Close()

    End Sub

    Protected Sub Button36_Click(sender As Object, e As EventArgs) Handles Button36.Click

        'Adds a focus attribute to factory installed options sush that they *wont* appear (for an channld that's not focussing on FIOs)
        Import.FIOfocus()

    End Sub

    Protected Sub Button37_Click(sender As Object, e As EventArgs) Handles Button37.Click

        Import.Legal()
    End Sub

    Protected Sub Button38_Click(sender As Object, e As EventArgs) Handles Button38.Click

        Dim avariant As clsVariant

        Dim unhosted As clsChannel = iq.i_channel_code("UNHOSTED")
        Dim nvid As Integer = 0
        Dim npid As Integer = 0

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim vwc As DataTable = da.MakeWriteCacheFor(con, "Variant", nvid, True)
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Price", npid, True)

        Dim xw As clsRegion = iq.i_region_code("XW")

        Dim c As Integer = 1
        For Each product In iq.Products.Values
            avariant = New clsVariant("TST", product, unhosted, "FAKE" & c, "*Test variant*", "CCC", "#UK", xw, False, vwc, nvid)

            Dim pr As NullablePrice = New NullablePrice(CSng(100), iq.i_currency_code("GBP"), False)
            'Dim prc As clsPrice = New clsPrice(avariant, iq.getPriceBand(""), pr, "fake", pwc, npid)
            c += 1
        Next

        da.BulkWrite(con, vwc, "Variant")
        da.BulkWrite(con, pwc, "Price")

        con.Close()

        Beep()

    End Sub

    Protected Sub btnImportQuotes_Click(sender As Object, e As EventArgs) Handles btnImportQuotes.Click
        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim errorMessages As New List(Of String)
        quoteImport.QuotesByHostID(con, txtHostID2.Text, errorMessages)
        If errorMessages.Count > 0 Then
            Label9.Text = Join(errorMessages.ToArray(), "###")
        End If
    End Sub

    Protected Sub btnIncImport_Click(sender As Object, e As EventArgs)

    End Sub

    Protected Sub btnSpecificImport_Click(sender As Object, e As EventArgs) Handles btnSpecificImport.Click
        Dim retString As String = ""
        Select Case txtSpecificImport.Text
            Case "SoftwareSlots"
                Dim rdr As SqlClient.SqlDataReader
                Dim sql$ = "SELECT sysType,modelSKU,cpu,cpuqty,ram,ramqty,pristor,pristorqty,secstor,secstorqty,terstor,terstorqty,raid,raidcache,psu,psuqty,iloLicense,iloHardware,iceIncluded,"
                sql$ &= "WLAN,WWAN,[controllers],extras,software,discreteGraphics,options"
                sql$ &= " FROM h3.[iq].products.union_systems "

                rdr = da.DBExecuteReader(da.OpenDatabase(), sql)

                While (rdr.Read)
                    If iq.i_SKU.ContainsKey(rdr("modelsku")) Then
                        Dim sys = iq.i_SKU(rdr("modelsku"))
                        For Each one In Split("software,extras,iceIncluded,options", ",")
                            If Not IsDBNull(rdr.Item(one)) Then
                                For Each s In Split(rdr.Item(one), ",")
                                    If s = "727258-B21" Then
                                        Dim a = 9
                                    End If
                                    For Each sysb In sys.Branches '.Values 'assuming only one path...
                                        Dim resultPath As String = ""
                                        If sysb.AllPaths.Count > 0 Then
                                            Dim childb = sysb.findChildBySKU2(sysb.AllPaths.First, s, resultPath)
                                            If childb IsNot Nothing Then
                                                If childb.Quantities.Where(Function(q) (q.Value.Path = "" OrElse q.Value.Path = resultPath) AndAlso q.Value.FOC = True AndAlso q.Value.NumPreInstalled = 1).Count = 0 Then
                                                    Dim q = New clsQuantity(iq.i_region_code("XW"), resultPath, childb, 1, 1, 1, True)
                                                    retString &= "Adding For: " & s & "-" & childb.ID & "-" & resultPath & Environment.NewLine
                                                Else
                                                    retString &= "Already exists for: " & s & "-" & childb.ID & "-" & resultPath & Environment.NewLine
                                                End If
                                            Else
                                                retString &= "Part Missing: " & s & Environment.NewLine
                                            End If
                                        End If
                                    Next
                                Next
                            End If
                        Next
                    End If
                End While

            Case "Incomat" 'Check IQ1 incompatible field has come across for everything and then prune based on this

                'Make sure we have all the attributes...
                Dim sql = "select optsku,incompatible from h3.iq.products.options where incompatible is not null"
                Dim rdr = da.DBExecuteReader(da.OpenDatabase(), sql)
                While (rdr.Read)
                    If iq.i_SKU.ContainsKey(rdr("optsku")) Then
                        If Not iq.i_SKU(rdr("optsku")).i_Attributes_Code.ContainsKey("incompat") Then
                            Dim n = New clsProductAttribute(iq.i_SKU(rdr("optsku")), iq.i_attribute_code("incompat"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr("incompatible").ToString(), English, "", 0, Nothing, 0, False))
                        End If
                    End If
                End While

                For Each p In iq.Products.Values

                    If p.i_Attributes_Code.ContainsKey("incompat") Then
                        'We have a product with incompatibility
                        For Each b In p.Branches '.Values
                            For Each path In b.AllPaths
                                Dim fm As String = ""
                                Dim fbs = findFamily(path, fm, True, False)
                                Dim incompat As List(Of String) = New List(Of String)()
                                For Each il In p.i_Attributes_Code("incompat").Select(Function(aa) aa.Translation.text(English))
                                    incompat.AddRange(Split(il, ","))
                                Next
                                If incompat.Where(Function(ic) ic.ToLower = fm.ToLower OrElse ic.ToLower = fbs.ToLower).Count > 0 Then 'contains 
                                    'We have an incompat path
                                    If b.Prunes.Where(Function(pr) pr.Value.Path = path).Count = 0 Then
                                        Dim pr = New clsPrune(path, New NullableInt(), "IncompatDef")
                                    End If
                                End If
                            Next
                        Next
                    End If
                Next
            Case "CheckFamSlots"
                Dim errorMessages As List(Of String) = New List(Of String)()
                Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily()

                If iq.i_slotType_Code.ContainsKey("VGA") OrElse iq.i_slotType_Code("VGA").ContainsKey("VIDEO_DISPLAYS") Then
                    Dim ff = New clsSlotType("VGA", "VIDEO_DISPLAYS", iq.AddTranslation("Video Display", English, "", 0, Nothing, 0, False))
                End If
                If iq.i_slotType_Code.ContainsKey("VGA") OrElse iq.i_slotType_Code("VGA").ContainsKey("VIDEO_DEVICES_SERVERS") Then
                    Dim ff = New clsSlotType("VGA", "VIDEO_DEVICES_SERVERS", iq.AddTranslation("Video Devices Servers", English, "", 0, Nothing, 0, False))
                End If

                If iq.i_slotType_Code.ContainsKey("VGA") OrElse iq.i_slotType_Code("VGA").ContainsKey("VIDEO_ADAPTERS") Then
                    Dim ff = New clsSlotType("VGA", "VIDEO_ADAPTERS", iq.AddTranslation("Video Adapter", English, "", 0, Nothing, 0, False))
                End If
                If iq.i_slotType_Code.ContainsKey("FAN") OrElse iq.i_slotType_Code("FAN").ContainsKey("NHP_FAN") Then
                    Dim ff = New clsSlotType("FAN", "NHP_FAN", iq.AddTranslation("Non-HotPlug FAN", English, "", 0, Nothing, 0, False))
                End If
                If iq.i_slotType_Code.ContainsKey("FAN") OrElse iq.i_slotType_Code("FAN").ContainsKey("FAN_NHP") Then
                    Dim ff = New clsSlotType("FAN", "FAN_NHP", iq.AddTranslation("Non-HotPlug FAN", English, "", 0, Nothing, 0, False))
                End If
                If iq.i_slotType_Code.ContainsKey("PSU") OrElse iq.i_slotType_Code("PSU").ContainsKey("SAN_POWER") Then
                    Dim ff = New clsSlotType("PSU", "SAN_POWER", iq.AddTranslation("SAN Power Supply", English, "", 0, Nothing, 0, False))
                End If


                Dim dt = da.FilledDataTable(da.OpenDatabase(), "select * from h3.iq.products.optionlimits")
                For Each b In iq.Branches.Values
                    If b.Product IsNot Nothing AndAlso b.Product.isSystem Then
                        If b.Product.SKU = "J8Z43EA" Then
                            Dim f = 8
                        End If
                        For Each Path In b.AllPaths

                            Dim fm As String = ""
                            findFamily(Path, fm, True, True)
                            Dim rs = dt.Select("SysFamily = '" & fm & "'")
                            For Each r In rs
                                If Not iq.i_slotType_Code.ContainsKey(r("OptTYpe")) Then Continue For
                                Dim relevantslots = b.slots.Where(Function(sl) sl.Value.Type.MajorCode.ToLower = r("OptType").ToString.ToLower AndAlso (sl.Value.path = "" OrElse sl.Value.path = Path)).ToList()
                                'Add chassis slots
                                Dim cb As clsBranch
                                For Each c In b.childBranches.Values
                                    If c.Translation.text(English).Contains(" chassis") Then
                                        cb = c
                                        relevantslots.AddRange(c.slots.Where(Function(sl) sl.Value.Type.MajorCode.ToLower = r("OptType").ToString.ToLower AndAlso (sl.Value.path = "" OrElse sl.Value.path = Path)))
                                    End If
                                Next
                                If relevantslots.Count = 0 AndAlso CInt(r("QtyMax")) > 0 Then
                                    'Add then slot
                                    Dim minT = ""
                                    If Not ofDic(fm).ContainsKey(r("OptType").ToString) Then
                                        If iq.i_slotType_Code(r("opttype")).Count = 1 Then
                                            If Not iq.i_slotType_Code(r("OptType")).ContainsKey("GEN") Then

                                                minT = iq.i_slotType_Code(r("opttype")).First.Value.MinorCode

                                            End If

                                        Else
                                            Dim st2 = New clsSlotType(r("OptType"), "GEN", iq.AddTranslation(r("opttype"), English, "", 0, Nothing, 0, False))
                                            minT = "GEN"
                                        End If

                                    Else
                                        minT = ofDic(fm)(r("OptType").ToString)
                                    End If
                                    If iq.i_slotType_Code.ContainsKey(r("opttype").ToString()) AndAlso r("opttype") <> "FAN" AndAlso r("opttype") <> "MEM" Then 'we dont want fan and memory is on the CPU in some cases so its too dangerous here
                                        If minT = "POWER_NBK" Or minT = "POWER_PBK" Then
                                            If Not iq.i_slotType_Code("PSUm").ContainsKey(ofDic(fm)(r("OptType").ToString)) Then
                                                Dim sl = New clsSlotType("PSUm", ofDic(fm)(r("OptType").ToString), iq.AddTranslation("ProBook Power Supply", English, "", 0, Nothing, 0, False))
                                            End If
                                            Dim slott = New clsSlot(iq.i_slotType_Code("PSUm")(ofDic(fm)(r("OptType").ToString)), cb, "", CInt(r("QtyMax")), Nothing, New NullableInt(), r("Incr_min"), If(IsDBNull(r("incr_pref")), 0, CInt(r("incr_pref"))))
                                            retString &= "Added: PSUm -" & ofDic(fm)(r("OptType").ToString) & "-" & b.Product.SKU & Environment.NewLine
                                        Else
                                            Dim slot = New clsSlot(iq.i_slotType_Code(r("opttype").ToString())(minT), cb, "", CInt(r("QtyMax")), Nothing, New NullableInt(), r("Incr_min"), If(IsDBNull(r("incr_pref")), 0, CInt(r("incr_pref"))))
                                            retString &= "Added: " & r("opttype").ToString() & "-" & minT & "-" & b.Product.SKU & Environment.NewLine
                                        End If

                                    End If
                                End If
                                If relevantslots.Count > 0 AndAlso CInt(r("QtyMax")) = 0 Then
                                    'Remove
                                    If {"POEP", "OPT", "SFPP"}.Contains(r("opttype")) Then
                                        relevantslots.First.Value.delete(errorMessages)
                                        retString &= "DELETE: " & b.Product.SKU & Environment.NewLine
                                    End If
                                End If
                                'Work on diff numbers...

                            Next
                        Next
                    End If


                Next
            Case "OptionSlots"
                Dim errorMessages As List(Of String) = New List(Of String)()
                Dim sql = "select optsku,slots,opttype,optfamily from h3.iq.products.options"
                Dim rdr = da.DBExecuteReader(da.OpenDatabase(), sql)
                While (rdr.Read)
                    If iq.i_SKU.ContainsKey(rdr("optsku")) Then
                        If iq.i_slotType_Code.ContainsKey(rdr("opttype")) AndAlso iq.i_slotType_Code(rdr("opttype")).ContainsKey(rdr("optfamily")) Then
                            Dim prod = iq.i_SKU(rdr("optsku"))
                            For Each branch In prod.Branches '.Values
                                Dim sl = branch.slots.Values.Where(Function(br) br.Type.MajorCode.ToLower = rdr("opttype").ToString.ToLower) ' removed AndAlso br.Type.MinorCode.ToLower = rdr("optfamily").ToString.ToLower for CPU's mainly
                                Dim slcount = sl.Sum(Function(d) d.numSlots)
                                If CInt(rdr("slots")) > 0 And slcount = 0 Then
                                    'Need to add
                                    Dim newslot = New clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), branch, "", -rdr("slots"), Nothing, New NullableInt(), 0, 0, Nothing)
                                    retString &= "Adding: " & rdr("optsku") & "-" & rdr("opttype") & "." & rdr("optfamily") & Environment.NewLine
                                ElseIf CInt(rdr("slots")) = 0 And slcount > 0 Then
                                    For Each s In sl
                                        s.delete(errorMessages)
                                    Next
                                    retString &= "Removed: " & rdr("optsku") & "-" & rdr("opttype") & "." & rdr("optfamily") & Environment.NewLine
                                    'Remove
                                ElseIf -CInt(rdr("slots")) <> slcount Then
                                    'Need to amend
                                    '''''How?
                                    Dim a = 9
                                    retString &= "Altered: " & rdr("optsku") & "-" & rdr("opttype") & "." & rdr("optfamily") & Environment.NewLine
                                End If


                            Next
                        Else
                            'Slot doesnt exist
                            retString &= "SlotType: " & rdr("opttype") & "." & rdr("optfamily") & Environment.NewLine
                        End If
                    End If

                End While
            Case "FixLocalisation"

                Dim sql = "select optsku,aaonly,localisation from h3.iq.products.options where localisation is not null"
                Dim rdr = da.DBExecuteReader(da.OpenDatabase(), sql)
                While (rdr.Read)
                    If iq.i_SKU.ContainsKey(rdr("optsku")) Then
                        Dim optionproduct = iq.i_SKU(rdr("optsku"))

                        Dim rgns As String = ""
                        If Not IsDBNull(rdr.Item("localisation")) Then rgns = rdr.Item("localisation")

                        If rdr.Item("aaonly") <> 0 Then
                            rgns &= ",AA"
                        End If

                        Dim regions As List(Of clsRegion) = New List(Of clsRegion)
                        If rgns <> "" Then
                            If optionproduct IsNot Nothing Then


                                Dim cs As List(Of String) = Split(rgns, ",").ToList

                                If Not cs.Contains("XW") Then   'Anything paul has localized 'worldwide' needs no restriction

                                    cleanRegions(cs, clsRegion.containment)
                                    For Each c In cs

                                        If c = "UCSA" Then c = "USCA" 'fix a typo
                                        If iq.i_region_code.ContainsKey(c) Then
                                            regions.Add(iq.i_region_code(c))
                                        Else
                                            Logit("invalid region " & c & " (in products.options.localisation)")
                                            '    Stop
                                        End If
                                    Next
                                End If
                            End If
                        End If
                        'Create qty
                        For Each branch In optionproduct.Branches
                            If branch.Quantities.Count = 0 Then
                                'For Each Path In branch.AllPaths
                                For Each reg In regions
                                    Dim q = New clsQuantity(reg, "", branch, 0, 1, 0, False, Nothing)
                                Next
                                'Next
                            End If
                        Next

                    End If
                End While
            Case "FixProductLocalisation"

                Dim sql = "select ModelSKU,ActiveSites,AAOnly from h3.iq.products.systems where ActiveSites is not null and modelsku='J8Q75EA'"
                Dim rdr = da.DBExecuteReader(da.OpenDatabase(), sql)

                While (rdr.Read)

                    If iq.i_SKU.ContainsKey(rdr("ModelSKU")) Then

                        Dim optionProduct = iq.i_SKU(rdr("ModelSKU"))
                        Dim rgns As String = rdr.Item("ActiveSites")

                        If rdr.Item("AAOnly") <> 0 Then
                            rgns &= ",AA"
                        End If

                        Dim regions As List(Of clsRegion) = New List(Of clsRegion)
                        If rgns <> "" Then
                            If optionProduct IsNot Nothing Then

                                Dim regionList As List(Of String) = Split(rgns, ",").ToList

                                cleanRegions(regionList, clsRegion.containment)
                                For Each r In regionList

                                    If r = "UCSA" Then r = "USCA" 'fix a typo
                                    If iq.i_region_code.ContainsKey(r) Then
                                        regions.Add(iq.i_region_code(r))
                                    Else
                                        Logit("invalid region " & r & " (in products.systems.ActiveSites)")
                                        '    Stop
                                    End If

                                Next
                            End If
                        End If

                        'Create qty
                        For Each branch In optionProduct.Branches
                            If branch.Quantities.Count = 0 Then
                                'For Each Path In branch.AllPaths
                                For Each reg In regions
                                    Dim q = New clsQuantity(reg, "", branch, 0, 1, 0, False, Nothing)
                                Next
                                'Next
                            End If
                        Next

                    End If
                End While
            Case "ReorderL3s"
                'We are only interested in rack and power options, turns out we arent, we are interested in ALL L3's
                Dim con = da.OpenDatabase()

                Dim SQLtoRun As List(Of String) = New List(Of String)()
                Dim toRemove As List(Of clsBranch) = New List(Of clsBranch)()

                Dim errorMessages As List(Of String) = New List(Of String)()
                Dim dr = da.DBExecuteReader(con, "SELECT * FROM h3.iq.dbo.V2_OPtionCatsml WHERE L3 is not null") ' = 'Rack &amp; Power'
                While dr.Read
                    If Trim(dr("l3real")).ToLower = "data cables" Then
                        Dim a = 9
                    End If
                    If iq.i_SKU.ContainsKey(dr("optsku")) Then
                        Dim optionProduct As clsProduct = iq.i_SKU(dr("optsku"))
                        For Each branch In optionProduct.Branches

                            If branch.AllPaths.Count > 0 AndAlso Not optionProduct.isSystem(branch.AllPaths.First) Then 'Make sure we dont move the system branches which are also options (swicthes etc)
                                Dim success As Boolean = False

                                'Ok here we are look at the branch above
                                'Determine which level the L3 branch falls
                                Dim l2Branch As clsBranch = Nothing
                                Dim l3Branch As clsBranch = Nothing
                                Select Case "All Options"
                                    Case branch.Parent.Parent.Translation.text(English)
                                        'Its hung off an L1 branch, somethig wrong...!
                                    Case If(branch.Parent.Parent.Parent IsNot Nothing, branch.Parent.Parent.Parent.Translation.text(English), "")
                                        'L2 so we need to move to L3
                                        l2Branch = branch.Parent
                                    Case If(branch.Parent.Parent.Parent IsNot Nothing AndAlso branch.Parent.Parent.Parent.Parent IsNot Nothing, branch.Parent.Parent.Parent.Parent.Translation.text(English), "")
                                        'L3 so check its the correct category
                                        l2Branch = branch.Parent.Parent
                                        l3Branch = branch.Parent
                                End Select

                                If l2Branch IsNot Nothing AndAlso (l3Branch Is Nothing OrElse l3Branch.Translation.text(English) <> dr("L3real")) Then
                                    'We have a miscategorisation
                                    'Find the correct parent...

                                    For Each child In l2Branch.childBranches
                                        If child.Value.Translation.text(English) = dr("L3real") Then
                                            'Reparent here
                                            branch.Parent = child.Value
                                            'branch.Value.Update(errorMessages)
                                            SQLtoRun.Add("UPDATE branch SET FK_Branch_ID_Parent = " & child.Value.ID & " WHERE id=" & branch.ID)
                                            success = True
                                            Exit For
                                        End If
                                    Next
                                    If Not success Then
                                        'Need to create one (no cache as it needs to be real time)
                                        Dim b = New clsBranch(Nothing, l2Branch, iq.AddTranslation(dr("L3real"), English, "l3", 0, Nothing, 0, False), "", iq.AddTranslation(dr("L3real"), English, "l3", 0, Nothing, 0, False), iq.AddTranslation(dr("L3real"), English, "l3", 0, Nothing, 0, False), Nothing, 0, 0, "G")
                                        branch.Parent = b
                                        'branch.Value.Update(errorMessages)
                                        SQLtoRun.Add("UPDATE branch SET FK_Branch_ID_Parent = " & b.ID & " WHERE id=" & branch.ID)
                                        success = True
                                    End If
                                    If l3Branch IsNot Nothing Then toRemove.Add(l3Branch)
                                End If
                            End If
                        Next
                    End If
                End While

                da.DBExecutesql(con, String.Join(";", SQLtoRun))

                For Each r In toRemove
                    If r.childBranches.Count = 0 Then
                        r.delete(errorMessages)
                    End If
                Next
            Case "De-dupSlots"
                'Look through chassis and make sure we dont have duplicates of EXACT slots as that really shouldnt be.  so check slottype maj and min and sign of slot -/+
                Dim errorMessages As List(Of String) = New List(Of String)()
                For Each p In iq.Products.Values
                    If p.isSystem Then
                        For Each branch In p.Branches
                            Dim currentSlots As Dictionary(Of String, clsSlot) = New Dictionary(Of String, clsSlot)
                            Dim deleteSlots As List(Of clsSlot) = New List(Of clsSlot)
                            For Each slot In branch.slots.Values
                                If Not currentSlots.ContainsKey(slot.Type.MajorCode & "^" & slot.Type.MinorCode & "^" & Math.Sign(slot.numSlots) & "^" & slot.slotNum.value) Then
                                    currentSlots.Add(slot.Type.MajorCode & "^" & slot.Type.MinorCode & "^" & Math.Sign(slot.numSlots) & "^" & slot.slotNum.value, slot)
                                Else
                                    'Mark for deletion
                                    If slot.Type.MajorCode <> "PCI" Then deleteSlots.Add(slot)
                                End If
                            Next
                            For Each child In branch.childBranches.Values
                                If child.Translation.text(English).Contains(" chassis") Then
                                    For Each slot In child.slots.Values
                                        If Not currentSlots.ContainsKey(slot.Type.MajorCode & "^" & slot.Type.MinorCode & "^" & Math.Sign(slot.numSlots) & "^" & slot.slotNum.value) Then
                                            currentSlots.Add(slot.Type.MajorCode & "^" & slot.Type.MinorCode & "^" & Math.Sign(slot.numSlots) & "^" & slot.slotNum.value, slot)
                                        Else
                                            'Mark for deletion
                                            If slot.Type.MajorCode <> "PCI" Then deleteSlots.Add(slot)
                                        End If
                                    Next
                                End If
                            Next

                            For Each slot In deleteSlots
                                slot.delete(errorMessages)
                            Next
                        Next
                    End If
                Next
            Case "FixPCIMinorSlots"
                Dim Sql2 = "SELECT familyname, dedisku from h3.[iq].products.SysFamilyPCIslots where dedicated=1"
                Dim rdr2 = da.DBExecuteReader(da.OpenDatabase(), Sql2)
                Dim ded As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))()
                While (rdr2.Read)
                    If Not IsDBNull(rdr2("dedisku")) Then
                        Dim s = Split(rdr2("dedisku"), ",")
                        For Each sku In s
                            If Not ded.ContainsKey(rdr2("familyname")) Then ded.Add(rdr2("familyname"), New List(Of String))
                            ded(rdr2("familyname")).Add(sku)
                        Next
                    End If
                End While

                '
                Dim Sql = "SELECT [SKU], [Code] ,[Notes]  FROM h3.[iQ].[products].[OptPCIcodes]"
                Dim rdr As IDataReader = da.DBExecuteReader(da.OpenDatabase(), Sql)
                Dim errorMessages As List(Of String) = New List(Of String)()
                While rdr.Read
                    If iq.i_SKU.ContainsKey(rdr("SKU")) Then
                        If rdr("sku") = "631670-B21" Then
                            Dim a = 9
                        End If
                        Dim product = iq.i_SKU(rdr("SKU"))
                        Dim minCode As String = rdr("Code")

                        minCode = Import.fixPci(minCode)

                        For Each branch In product.Branches
                            If branch.AllPaths.Count > 0 Then
                                Dim fam = findFamily(branch.AllPaths(0))
                                If fam <> "" Then
                                    Dim minCode2 = minCode & "_" & If(ded.ContainsKey(fam) AndAlso ded(fam).Contains(rdr("sku")), 1, 0)
                                    For Each slot In branch.slots.Values
                                        If slot.Type.MajorCode.ToUpper.StartsWith(Import.ConvertPCIMinorToMajor(minCode2).ToUpper) Then
                                            If Not iq.i_slotType_Code(slot.Type.MajorCode).ContainsKey(minCode2) Then
                                                Dim st = New clsSlotType(slot.Type.MajorCode, minCode2, iq.AddTranslation(Import.ExpandPCI(minCode2).fullText, English, "PCIST", 0, Nothing, 0, False))
                                            End If

                                            slot.Type = iq.i_slotType_Code(slot.Type.MajorCode)(minCode2)
                                            slot.update(errorMessages)
                                        End If
                                    Next
                                End If
                            End If
                        Next
                    End If
                End While

            Case "Fallbacks"
                'probably need to consider dedicated slots more carefully
                Dim sql = "delete from altslottype"
                Dim s = da.DBExecutesql(da.OpenDatabase(), sql)
                Dim ospec As pciStruct
                Dim ispec As pciStruct
                For Each ko In iq.SlotTypes.Keys
                    If iq.SlotTypes(ko).MinorCode = "PCIE_C8B8_HXHY_G2_0" Then
                        Dim a = -9
                    End If
                    If iq.SlotTypes(ko).Fallback IsNot Nothing Then iq.SlotTypes(ko).Fallback.Clear()
                    If UBound(Split(iq.SlotTypes(ko).MinorCode, "_")) = 4 Then 'necessary (had to hack it in during an import)
                        ospec = ExpandPCI(iq.SlotTypes(ko).MinorCode)
                        If Not ospec.dedicated Then
                            For Each ki In iq.SlotTypes.Keys
                                If iq.SlotTypes(ki).MinorCode = "PCIE_C16B8_HXFY_G3_0" Then
                                    Dim a = -9
                                End If
                                If Not ko = ki Then
                                    If Left(iq.SlotTypes(ko).MajorCode, 3) = "PCI" AndAlso Left(iq.SlotTypes(ki).MajorCode, 3) = "PCI" AndAlso iq.SlotTypes(ki).MinorCode = "GEN" Then
                                        Dim kos = 0
                                        If iq.SlotTypes(ko).MajorCode.Length > 3 Then kos = Asc(Right(iq.SlotTypes(ko).MajorCode, 1)) Else kos = 0
                                        Dim kis = 0
                                        If iq.SlotTypes(ki).MajorCode.Length > 3 Then kis = Asc(Right(iq.SlotTypes(ki).MajorCode, 1)) Else kis = 0

                                        If kis >= kos Then iq.SlotTypes(ko).AddFallback(iq.SlotTypes(ko).Fallback.Count, iq.SlotTypes(ki))
                                    Else
                                        If UBound(Split(iq.SlotTypes(ki).MinorCode, "_")) = 4 Then 'necessary (had to hack it in during an import)
                                            'If ki > ko Then
                                            ispec = ExpandPCI(iq.SlotTypes(ki).MinorCode)

                                            If ispec.tech = ospec.tech Or (Left(ispec.tech, 3) = "PCI" AndAlso Left(ospec.tech, 3) = "PCI") Then 'same 'technology' (PCIe/x/BLcm
                                                If ispec.connector >= ospec.connector Then 'slotform 1,8,16 - only add higher connector width slots
                                                    If ispec.speed >= ospec.speed Then 'Speed 4x 8x 16x - only use higher speed slots
                                                        If ispec.w >= ospec.w And ispec.h >= ospec.h Then 'only an alternative if same or wider AND same or higher
                                                            With iq.SlotTypes(ko)
                                                                .AddFallback(.Fallback.Count, iq.SlotTypes(ki))
                                                            End With
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            Case "ScanFIOs"

                Dim toUpdate = {"PSU", "RAM", "CPU"}

                Dim ck As String = ""
                Dim lDic As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)()
                For Each branch In iq.RootBranch.childBranches
                    If branch.Value.Translation.text(English).ToLower = "accessories and services" Then
                        branch.Value.BuildPathDic("", lDic, False)
                        Exit For
                    End If
                Next
                Dim con = da.OpenDatabase()
                Dim failed = New List(Of String)
                Dim dicOptLocalization As Dictionary(Of clsProduct, List(Of clsRegion)) = New Dictionary(Of clsProduct, List(Of clsRegion))()
                Import.LoadAbbreviations(con)

                Dim missing As List(Of String) = New List(Of String)()
                Dim sql = "select modelsku,psu,psuqty,ram,ramqty,cpu,cpuqty from h3.iq.products.systems where psu is not null or ram is not null or cpu is not null"
                Dim rdr = da.DBExecuteReader(da.OpenDatabase, sql)
                Dim errorMEssages As List(Of String) = New List(Of String)()
                While rdr.Read
                    If iq.i_SKU.ContainsKey(rdr("modelsku")) Then
                        Dim system_product = iq.i_SKU(rdr("modelsku"))

                        For Each opttype In toUpdate
                            If IsDBNull(rdr(opttype)) OrElse rdr(opttype) = "EXT" Then Continue For
                            If rdr(opttype) = "###MSM525_AP_IOC" Then
                                Dim a = 9
                            End If

                            If Not iq.i_SKU.ContainsKey(rdr(opttype).trim()) Then
                                'Product missing totally!
                                'Create it?
                                'Find the alloptions branch?

                                missing.Add(String.Format("Model: {0} missing {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine))

                                Dim column As String = opttype 'for clarity !! - will be psu ram ir cpu
                                If failed.Contains(rdr(column)) Then Continue While

                                Dim imp As New List(Of String) 'import the missing CPU/RAM/PSU
                                imp.Add(rdr.Item(column).trim)

                                'These are NOT the system options (it's the accessories catalogue)
                                If Import.optionsIncremental(da.OpenDatabase(), imp, iq.i_unit_code, lDic, dicOptLocalization, Nothing, True) Is Nothing Then
                                    failed.Add(rdr(opttype))
                                    Continue While
                                End If

                                If Not iq.i_SKU.ContainsKey(rdr(opttype)) Then
                                    failed.Add(rdr(opttype))
                                    missing.Add(String.Format("Model: {0} failed to create {1}{2}", rdr("ModelSKU"), rdr(opttype).trim(), Environment.NewLine))
                                    Continue While
                                End If

                                missing.Add(String.Format("Model: {0} added {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine))
                            End If
                            Dim psu_product = iq.i_SKU(rdr(opttype).trim())
                            For Each branch In system_product.Branches

                                For Each Path In branch.AllPaths
                                    Dim newpath As String = ""
                                    If branch.FindSystemAbove(Path, newpath) Is branch Then
                                        'Ok, lets find the quantity record...

                                        Dim validPaths As Dictionary(Of clsBranch, List(Of String)) = New Dictionary(Of clsBranch, List(Of String))

                                        For Each optBranch In psu_product.Branches

                                            For Each optPAth In optBranch.AllPaths
                                                If optPAth.Contains(Path) Then
                                                    'This is in here because there appear to be more than one branch for the same SKU under FIO's in [some] cases
                                                    If Not validPaths.ContainsKey(optBranch) Then validPaths.Add(optBranch, New List(Of String))
                                                    validPaths(optBranch).Add(optPAth)

                                                End If
                                            Next
                                        Next
                                        If validPaths.Count = 0 Then
                                            'Not mapped under this tree
                                            'Create a branch for it in all options / FIOs
                                            Dim ao = branch.ChildNamed("All Options")
                                            Dim fios = ao.ChildNamed("FIOs")
                                            Dim nb As clsBranch = New clsBranch(psu_product, fios, iq.AddTranslation(psu_product.SKU, English, "SKU", 0, Nothing, 0, True), "", iq.AddTranslation(psu_product.SKU, English, "SKU", 0, Nothing, 0, True), iq.AddTranslation(psu_product.SKU, English, "SKU", 0, Nothing, 0, True), Nothing, 0, False, "B", Nothing, 0)
                                            validPaths.Add(nb, New List(Of String) From {Path & "." & ao.ID & "." & fios.ID & "." & nb.ID})
                                            missing.Add(String.Format("Model: {0} added fio branch {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine))
                                        End If
                                        If validPaths.Keys.SelectMany(Function(vp) vp.Quantities).Where(Function(qr) validPaths(qr.Value.Branch).Contains(qr.Value.Path) Or String.IsNullOrEmpty(qr.Value.Path) And qr.Value.NumPreInstalled = If(IsDBNull(rdr(opttype & "qty")), 1, CInt(rdr(opttype & "qty")))).Count = 0 Then
                                            'Create it
                                            'Brutal now, if there is another preinstalled of a different sku and the same type remove it (has to be really as IQ1 only has one field for this)
                                            'Get the holder for this item
                                            Dim bb = iq.Branches(validPaths.First.Value.First.Split(".")(validPaths.First.Value.First.Split(".").Length - 1))
                                            Dim qtys = bb.childBranches.Values.SelectMany(Function(cb) cb.Quantities).Where(Function(qr) validPaths(qr.Value.Branch).Contains(qr.Value.Path) Or String.IsNullOrEmpty(qr.Value.Path) And qr.Value.NumPreInstalled > 0)
                                            For Each qtr In qtys
                                                qtr.Value.Delete(errorMEssages)
                                                missing.Add(String.Format("Model: {0} deleted quantity record for {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine))
                                            Next
                                            missing.Add(String.Format("Model: {0} added quantity record for {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine))
                                            Dim q = New clsQuantity(iq.i_region_code("XW"), validPaths.First.Value.First, validPaths.First.Key, If(IsDBNull(rdr(opttype & "qty")), 1, CInt(rdr(opttype & "qty"))), 0, 0, True, Nothing)
                                        End If
                                    End If
                                Next
                            Next
                        Next
                    End If
                End While
                retString = Join(missing.ToArray, "".ToArray)
            Case "RemoveGENs"
                Dim errorMessages As List(Of String) = New List(Of String)()
                For Each branch In iq.Branches.Values
                    'Dim branch = iq.Branches(2593)
                    If branch.Product IsNot Nothing AndAlso branch.Product.isSystem Then
                        If branch.AllPaths.Count > 0 Then
                            Dim slots = branch.slots.ToList()
                            Dim cb As clsBranch
                            For Each child In branch.childBranches.Values
                                If child.Translation.text(English).Contains(" chassis") Then
                                    slots.AddRange(child.slots.ToList())
                                    Exit For
                                End If
                            Next


                            Dim todelete = New List(Of clsSlot)
                            For Each path In branch.AllPaths
                                For Each slot In slots
                                    If slot.Value.Type.MajorCode.StartsWith("PCI") Then
                                        Dim a = 9
                                    End If
                                    Dim mc = Import.ConvertPCIMinorToMajor(slot.Value.Type.MinorCode)
                                    If (slot.Value.path.Contains(path) OrElse slot.Value.path = "") AndAlso ((slot.Value.Type.MajorCode.StartsWith("PCI") AndAlso mc = slot.Value.Type.MinorCode) Or Not slot.Value.Type.MajorCode.StartsWith("PCI")) Then
                                        If slots.Where(Function(st) (st.Value.path = "" OrElse st.Value.path.Contains(path)) AndAlso st.Value.Type.MajorCode = slot.Value.Type.MajorCode AndAlso st.Value.Type.MinorCode <> slot.Value.Type.MinorCode).Sum(Function(st) st.Value.numSlots) = slot.Value.numSlots Then
                                            retString &= "Branch (SKU): " & branch.ID & "(" & branch.Product.SKU & ") Removed Slot " & slot.Value.displayName(English) & " x " & slot.Value.numSlots & Environment.NewLine
                                            todelete.Add(slot.Value)
                                        End If
                                    End If
                                Next
                            Next
                            For Each sl In todelete
                                sl.delete(errorMessages)
                            Next
                        End If
                    End If
                Next
            Case "SoftwareSpec"
                Dim nextTKey = 0
                Dim con = da.OpenDatabase()
                Dim pawc = da.MakeWriteCacheFor(con, "ProductAttribute")
                Dim twc = da.MakeWriteCacheFor(con, "Translation", nextTKey, True)

                Dim Sql = "SELECT modelsku,isnull(translation,software) as software FROM h3.iq.products.systems left outer join h3.iq.dbo.Abbreviations a on a.code=systems.software where software is not null"
                Dim rdr As IDataReader = da.DBExecuteReader(da.OpenDatabase(), Sql)
                Dim errorMessages As List(Of String) = New List(Of String)()
                While rdr.Read
                    If iq.i_SKU.ContainsKey(rdr("modelsku")) Then
                        Dim prod = iq.i_SKU(rdr("modelsku"))
                        If Not prod.i_Attributes_Code.ContainsKey("software") Then
                            Dim at = New clsProductAttribute(prod, iq.i_attribute_code("software"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr("software"), English, "sw", 0, twc, nextTKey, True), pawc)
                        Else
                            prod.i_Attributes_Code("software").First.Translation.text(English) = rdr("software")
                            prod.i_Attributes_Code("software").First.Translation.Update(English)
                        End If
                    End If
                End While

                da.BulkWrite(con, twc, "Translation")
                da.BulkWrite(con, pawc, "ProductAttribute")
            Case "Expand OtherFeatures"
                Dim Sql = "SELECT code,translation from h3.iq.dbo.Abbreviations"
                Dim rdr As IDataReader = da.DBExecuteReader(da.OpenDatabase(), Sql)
                Dim errorMessages As List(Of String) = New List(Of String)()
                Dim Abrs = New Dictionary(Of String, String)
                While rdr.Read
                    Abrs.Add(rdr("code"), rdr("translation"))
                End While

                For Each prod In iq.Products.Values
                    If prod.i_Attributes_Code.ContainsKey("options") Then
                        Dim s = Split(prod.i_Attributes_Code("options").First.Translation.text(English), ",")
                        Dim nw = New List(Of String)
                        For Each element In s
                            If Abrs.ContainsKey(element) Then
                                nw.Add(Abrs(element))
                            Else
                                nw.Add(element)
                            End If
                        Next
                        prod.i_Attributes_Code("options").First.Translation.text(English) = Join(nw.ToArray, ",")
                        prod.i_Attributes_Code("options").First.Translation.Update(English)
                    End If
                Next
            Case "Split HK Support Screens" 'this one is a rather specific one off for HMC...
                Dim errorMEssages As List(Of String) = New List(Of String)()
                For Each branch In iq.Branches.Values
                    If branch.Translation.text(English) = "HW Support" Then
                        For Each Path In branch.AllPaths
                            Dim newpath = ""
                            Dim sysunit = branch.FindSystemAbove(Path, newpath)
                            If sysunit IsNot Nothing Then
                                If {"DTO", "NBK"}.Contains(sysunit.Product.ProductType.Code) Then
                                    branch.Matrix = iq.Screens(1812)
                                    branch.Update(errorMEssages)
                                End If

                            End If
                        Next

                    End If
                Next
            Case "RemoveGENPCIs"
                Dim errormessages = New List(Of String)
                For Each branch In iq.Branches.Values
                    Dim todelete As List(Of clsSlot) = New List(Of clsSlot)()
                    If branch.Product IsNot Nothing AndAlso branch.Product.isSystem Then
                        Dim chassisBranch As clsBranch
                        For Each child In branch.childBranches.Values
                            If child.Translation.text(English).Contains(" chassis") Then
                                chassisBranch = child
                                Exit For
                            End If
                        Next
                        For Each Path In branch.AllPaths
                            For Each slot In chassisBranch.slots.Values
                                If (String.IsNullOrEmpty(slot.path) OrElse slot.path = Path) Then
                                    If slot.Type.MajorCode.StartsWith("PCI") AndAlso Import.ConvertPCIMinorToMajor(slot.Type.MinorCode) = slot.Type.MinorCode Then
                                        If branch.slots.Values.Where(Function(bs) (String.IsNullOrEmpty(bs.path) OrElse bs.path = Path) AndAlso bs.Type.MajorCode.StartsWith("PCI")).Count > 0 Then
                                            todelete.Add(slot)
                                        End If
                                    End If
                                End If
                                If (String.IsNullOrEmpty(slot.path) OrElse slot.path = Path) AndAlso slot.Type.MajorCode = "CPU" Then
                                    todelete.Add(slot) ' Don't want any CPU's on the chassis!
                                End If
                            Next
                        Next
                    End If
                    For Each std In todelete
                        std.delete(errormessages)
                    Next
                Next
            Case "CheckAutoAdds"
                Dim sql = "SELECT [CountryCode],a.[ModelSKU],[AddSKU],[OptType],s.FamilyCode,a.ranking from h3.[iq].[Products].[AutoAdds] a inner join h3.iq.products.Systems s on a.modelsku=s.ModelSKU where opttype<>'WTY' and ranking = 1"
                Dim rdr As IDataReader = da.DBExecuteReader(da.OpenDatabase(), sql)
                Dim errorMessages As List(Of String) = New List(Of String)()
                While rdr.Read
                    If iq.i_SKU.ContainsKey(rdr("modelsku")) Then
                        Dim system As clsProduct = iq.i_SKU(rdr("modelsku"))
                        For Each systemBranch In system.Branches
                            For Each Path In systemBranch.AllPaths
                                Dim partPAth = ""
                                Dim part = systemBranch.findChildBySKU2(Path, rdr("addsku"), partPAth)
                                If part IsNot Nothing Then
                                    If part.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path = partPAth) AndAlso q.NumPreInstalled > 0 AndAlso q.Region.Encompasses(iq.i_region_code(rdr("CountryCode").replace("UK", "GB"))) AndAlso q.FOC = False).Count = 0 Then
                                        Dim q = New clsQuantity(iq.i_region_code(rdr("countrycode").replace("UK", "GB")), partPAth, part, 1, 0, 0, False, Nothing)
                                    End If
                                End If
                            Next
                        Next
                    End If
                End While
            Case "RemoveCPQs"
                Dim con = da.OpenDatabase()
                Dim errorMessages As List(Of String) = New List(Of String)()
                Dim ToDelete = New List(Of Int32)
                Dim torun As List(Of Int32) = New List(Of Int32)()
                For Each branch In iq.i_SpecialBranches("cpqroot").childBranches
                    'Grafts and prunes


                    ToDelete.Add(branch.Value.ID)
                    torun.Add(branch.Value.ID)
                Next
                'Split it!


                If torun.Count > 0 Then


                    da.DBExecutesql(con, "DELETE FROM graft WHERE FK_Branch_ID_Source IN (" & Join(ToDelete.Select(Function(f) f.ToString()).ToArray, ",") & ") OR FK_Branch_ID_Target IN (" & Join(ToDelete.Select(Function(f) f.ToString()).ToArray, ",") & ")")
                    While torun.Count > 0
                        Dim thischunk = torun.Take(10)
                        da.DBExecutesql(con, "DELETE FROM prune WHERE CALC_BRANCH_ID in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")")
                        da.DBExecutesql(con, "DELETE pa FROM productattribute pa inner join branch on branch.fk_product_id=pa.fk_product_id WHERE branch.id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")")

                        da.DBExecutesql(con, "DELETE FROM slot WHERE fk_branch_id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")")
                        da.DBExecutesql(con, "SELECT p.id INTO #tmp FROM product p inner join branch b on b.fk_product_id=p.id WHERE b.id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")" & _
                            "delete from quoteitem where fk_branch_id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ");" & _
                            "delete from quantity where FK_branch_ID in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ");" & _
                            "delete b FROM branch b WHERE b.id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ");" & _
                            "delete s from stock s inner join variant on fk_variant_id=variant.id where fk_product_id in (select id from #tmp);" & _
                            "delete from variant where FK_Product_ID in (select id from #tmp);" & _
                            "delete from product where id in (select id from #tmp);" & _
                            "drop table #tmp")
                        'da.DBExecutesql(con, "DELETE FROM branch WHERE id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")")

                        torun.RemoveAll(Function(m) thischunk.Contains(m))
                    End While
                End If

                'For Each td In ToDelete
                '    Dim todelattr = New List(Of clsProductAttribute)
                '    For Each pa In iq.Branches(td).Product.Attributes.Values
                '        todelattr.Add(pa)
                '    Next
                '    For Each tdp In todelattr
                '        tdp.delete(errorMessages)
                '    Next
                '    iq.Branches(td).Product.delete(errorMessages)
                '    Dim todelslot = New List(Of clsSlot)
                '    For Each slot In iq.Branches(td).slots.Values
                '        todelslot.Add(slot)
                '    Next
                '    For Each tds In todelslot
                '        tds.delete(errorMessages)
                '    Next
                '    iq.Branches(td).delete(errorMessages)
                'Next
            Case "FixSystemasOptionL3ReorderCockup"
                Dim con = da.OpenDatabase()
                Dim sqltorun = New List(Of String)
                Dim dr = da.DBExecuteReader(con, "SELECT * FROM h3.iq.dbo.V2_OPtionCatsml WHERE L3 is not null") ' = 'Rack &amp; Power'
                While dr.Read
                    If iq.i_SKU.ContainsKey(dr("optsku")) Then
                        Dim prod = iq.i_SKU(dr("optsku"))
                        For Each branch In prod.Branches
                            If branch.Product IsNot Nothing Then
                                For Each Path In branch.AllPaths
                                    If branch.Product.isSystem(Path) Then
                                        'We have one here which we shouldnt have moved!
                                        'Find where it should be...
                                        Dim cat = branch.Parent.Parent 'HP Networking or the like
                                        For Each childbranch In cat.childBranches.Values
                                            If childbranch.Product IsNot Nothing AndAlso childbranch.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                                                If prod.i_Attributes_Code.ContainsKey("FamMajor") Then

                                                    If childbranch.Product.i_Attributes_Code("FamMajor").First.Translation.text(English) = prod.i_Attributes_Code("FamMajor").First.Translation.text(English) Then
                                                        'Found it, reparent
                                                        branch.Parent = childbranch
                                                        sqltorun.Add("UPDATE branch SET FK_Branch_ID_Parent = " & childbranch.ID & " WHERE id=" & branch.ID)
                                                    End If
                                                Else
                                                    If prod.i_Attributes_Code.ContainsKey("FamMinor") Then
                                                        If childbranch.childBranches.Count > 0 AndAlso childbranch.childBranches.Values.First.Product.i_Attributes_Code("FamMinor").First.Translation.text(English) = prod.i_Attributes_Code("FamMinor").First.Translation.text(English) Then
                                                            'Found it, reparent
                                                            branch.Parent = childbranch
                                                            sqltorun.Add("UPDATE branch SET FK_Branch_ID_Parent = " & childbranch.ID & " WHERE id=" & branch.ID)
                                                        Else
                                                            retString &= " Cannot place: " & prod.SKU & Environment.NewLine
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        Next

                                    End If
                                Next
                            End If
                        Next
                    End If
                End While
                If sqltorun.Count > 0 Then da.DBExecutesql(con, String.Join(";", sqltorun))
            Case "AddFIODependantSlotsToChassis"
                'Scan the system to see if there are any FIO's which cause instant slot validations (shouldn't happen EVER)
                Dim errorMessages = New List(Of String)
                For Each branch In iq.Branches.Values
                    If branch.Product IsNot Nothing AndAlso branch.Product.isSystem Then
                        Dim cb As clsBranch
                        For Each childbranch In branch.childBranches.Values
                            If childbranch.Translation.text(English).Contains(" chassis") Then
                                cb = childbranch
                                Exit For
                            End If
                        Next

                        For Each Path In branch.AllPaths
                            Dim sl = branch.slotsInForce(Path).ToDictionary(Function(sif) sif, Function(sif) 1)
                            Dim sl2 As Dictionary(Of clsSlot, Int32) = New Dictionary(Of clsSlot, Integer)()
                            For Each fio In branch.GetPreInstalledRecursive(iq.i_region_code("XW"), Path, errorMessages)
                                For Each slot In fio.Branch.slots.Values
                                    If String.IsNullOrEmpty(slot.path) OrElse slot.path.Contains(Path) Then
                                        'Check for weird ones (like systems with systems)
                                        If Not sl.ContainsKey(slot) And Not sl2.ContainsKey(slot) Then
                                            sl2.Add(slot, fio.NumPreInstalled)
                                        Else
                                            retString &= "Unable to add all slots, odd config for: " & branch.Translation.text(English) & Environment.NewLine
                                        End If
                                    End If
                                Next
                            Next
                            For Each slot In sl
                                If slot.Key.Type.MajorCode <> "PWR" AndAlso (sl.Where(Function(inf) inf.Key.NonStrictType Is slot.Key.NonStrictType).Sum(Function(inf) inf.Key.numSlots) + sl2.Where(Function(inf) inf.Key.NonStrictType Is slot.Key.NonStrictType).Sum(Function(inf) inf.Key.numSlots)) < 0 Then
                                    'Is it validated, do we care?
                                    Dim vi = iq.ValidationInclusions.Where(Function(vai) vai.Value.MajorCode.ToLower = slot.Key.Type.MajorCode.ToLower).FirstOrDefault
                                    If vi.Value IsNot Nothing AndAlso vi.Value.InclusionType = enumInclusionType.Validated Then
                                        Dim a = 9
                                        Dim sq = -sl.Where(Function(inf) inf.Key.NonStrictType Is slot.Key.NonStrictType).Sum(Function(inf) inf.Key.numSlots)
                                        Dim slo = New clsSlot(slot.Key.Type, cb, "", sq, Nothing, New NullableInt(), 0, 0, Nothing)
                                        sl2.Add(slo, 1)
                                        retString &= "Added slot for: " & branch.Translation.text(English) & " of type: " & slot.Key.Type.MajorCode & " qty " & sq & Environment.NewLine
                                    End If
                                End If

                            Next
                        Next


                    End If
                Next
            Case "ScanForUnattachedQuantites"
                Dim errormessages = New List(Of String)
                For Each branch In iq.Branches.Values
                    Dim todelete = New List(Of clsQuantity)
                    Dim toupdate = New List(Of clsQuantity)
                    For Each quantity In branch.Quantities.Values
                        If Not String.IsNullOrEmpty(quantity.Path) Then
                            If Not branch.AllPaths.Contains(quantity.Path) Then
                                'Whoa orphaned quantity then... what to do
                                'Well, check we still have the info on its... replacement?  how do we know where its replacement is?

                                If branch.Product.ProductType.Code.ToLower = "wty" Then
                                    todelete.Add(quantity)
                                Else
                                    'Check last but one bit
                                    Dim segs = Split(quantity.Path, ".")
                                    Dim holdingseg = segs(segs.Length - 2)
                                    Dim newpath = ""
                                    For Each Path In branch.AllPaths
                                        Dim bpsegs = Split(Path, ".")
                                        For i = 0 To bpsegs.Length
                                            If bpsegs(i) <> segs(i) Then
                                                If i = bpsegs.Length - 2 Then
                                                    'We have a possible l3 change...
                                                    If bpsegs.Last = segs.Last Then
                                                        newpath = Path 'Left(Path, Path.IndexOf(bpsegs(i))) & segs(i) & "." & segs.Last
                                                        Exit For
                                                    End If
                                                Else
                                                    Exit For
                                                End If
                                            End If
                                        Next
                                        If newpath <> "" Then
                                            If branch.Quantities.Values.Where(Function(q) q.Path = newpath).Count > 0 Then
                                                Dim a = 9
                                            End If
                                            quantity.Path = newpath
                                            toupdate.Add(quantity)

                                            'Update?
                                            Exit For
                                        End If
                                    Next
                                    If newpath = "" Then
                                        'Not found a replacement
                                        Dim a = 9
                                    End If
                                End If
                            End If
                        End If
                    Next
                    For Each u In toupdate
                        u.update(errormessages)
                    Next
                    For Each t In todelete
                        t.Delete(errormessages)
                    Next
                Next
            Case "AddCarePackSlots"
                For Each branch In iq.Branches.Values
                    If branch.Product IsNot Nothing AndAlso branch.Product.isSystem Then
                        If {"SWD", "HPN"}.Contains(branch.Product.ProductType.Code) Then
                            Dim cb As clsBranch
                            For Each childbranch In branch.childBranches.Values
                                If childbranch.Translation.text(English).Contains(" chassis") Then
                                    cb = childbranch
                                    Exit For
                                End If
                            Next
                            If cb.slots.Values.Where(Function(sl) sl.Type.MajorCode.ToUpper = "WTY").Count = 0 Then
                                Dim slt = New clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), cb, "", 3, Nothing, New NullableInt(), 0, 0)
                            End If
                        End If
                    End If
                Next
            Case "RemoveMedSlots"
                Dim errormessages = New List(Of String)
                For Each b In iq.Branches.Values
                    If b.Product IsNot Nothing AndAlso Not b.Product.isSystem Then
                        If b.Product.ProductType.Code = "MED" Then
                            If b.i_Slots.ContainsKey("MED") Then
                                Dim hasSlots = New List(Of clsSlotType)
                                Dim todelete = New List(Of clsSlot)
                                For Each sl In b.slots.Values
                                    If sl.numSlots > 0 Then
                                        todelete.Add(sl)
                                    Else
                                        If hasSlots.Contains(sl.Type) Then
                                            todelete.Add(sl)
                                        Else
                                            hasSlots.Add(sl.Type)
                                        End If
                                    End If
                                Next
                                For Each d In todelete
                                    d.delete(errormessages)
                                Next
                            End If
                        End If
                    End If
                Next
        End Select

        txtOutput.Text = retString
    End Sub

    Protected Sub Button39_Click(sender As Object, e As EventArgs) Handles Button39.Click

        Dim ErrorMessages As List(Of String) = New List(Of String)
        Response.Write("DONE:" & Import.Tokens(ErrorMessages))
        OutputErrors(Form.Controls, ErrorMessages, 0, True)

    End Sub

    Protected Sub BtnSetCur_Click(sender As Object, e As EventArgs) Handles BtnSetCur.Click

        Dim errs As List(Of String) = New List(Of String)


        Dim c As clsCurrency
        If iq.i_currency_code.ContainsKey(TxtCurr.Text) Then
            c = iq.i_currency_code(TxtCurr.Text)

            If txtHost4Curr.Text <> "" Then
                Import.fixCurrencies(txtHost4Curr.Text, c, errs)

            Else
                errs.Add("Please enter a hostid")
            End If


        Else
            errs.Add("No such currency")
        End If

        OutputErrors(Panel2.Controls, errs, 0)
    End Sub

    Protected Sub Button40_Click(sender As Object, e As EventArgs) Handles Button40.Click

        Dim errormessages As List(Of String) = New List(Of String)
        Import.clones(errormessages)

        For Each Er In errormessages
            Form.Controls.Add(ErrorDymo(Er))
        Next


    End Sub

    Protected Sub btnExpScreen_Click(sender As Object, e As EventArgs) Handles btnExpScreen.Click
        Dim err As List(Of String)
        Dim screen As clsScreen = iq.i_screens_code("HDD").copy(err)
        screen.title = "Export CSV"
        screen.code = "ExCSV"
        Dim screenfield As clsField = New clsField(screen, "Product.ProductType.Code", "", iq.AddTranslation("Product Type Code", English, "FLDLBL", 1, Nothing, 0, False), "Product Type Code", Nothing, iq.i_inputType_code("string"), 10, 1, 10, 10, "", True, False, "", "", 1, Nothing, "", False, Nothing, True)
        Dim screenfield2 As clsField = New clsField(screen, "Product.ProductType.Translation", "", iq.AddTranslation("Product Type Name", English, "FLDLBL", 1, Nothing, 0, False), "Product Type Name", Nothing, iq.i_inputType_code("string"), 10, 1, 10, 10, "", True, False, "", "", 1, Nothing, "", False, Nothing, True)
        screen.Update(err)
        Dim strfieldsnotrequired As String = "Product.i_Attributes_Code(capacity)(0)	Product.i_Attributes_Code(speed)(0)	Parent.Translation	Parent.Parent.Translation"

        For Each fld In screen.Fields.Values
            If strfieldsnotrequired.Contains(fld.displayName(English)) Then
                fld.visibleList = False
            Else
                Select Case fld.displayName(English)
                    Case "Product.ProductType.Code"
                        fld.order = 1
                    Case "Product.ProductType.Translation"
                        fld.order = 10
                    Case "Product.i_Attributes_Code(MfrSKU)(0)"
                        fld.order = 15
                    Case "Product.i_Attributes_Code(Desc)(0)"
                        fld.order = 20
                    Case "Stock"
                        fld.order = 25
                    Case "CustomerPrice"
                        fld.order = 30
                End Select
            End If
            fld.update(err)

        Next

    End Sub

    Protected Sub Button41_Click(sender As Object, e As EventArgs) Handles Button41.Click
        Dim psu As List(Of clsProduct) = (From p In iq.Products.Values Where p.ProductType.Code.ToUpper = "PSU").ToList()

        Dim stringPsu As String
        For Each psuUnit In psu
            If psuUnit.i_Attributes_Code.ContainsKey("Desc") Then
                If psuUnit.i_Attributes_Code.ContainsKey("capacity") Then
                    stringPsu = stringPsu & psuUnit.SKU & " : TRUE :" & psuUnit.i_Attributes_Code("Desc")(0).Translation.text(English) & ": " & psuUnit.i_Attributes_Code("capacity")(0).NumericValue & " <br/>"
                Else

                    stringPsu = stringPsu & psuUnit.SKU & " : FALSE :" & psuUnit.i_Attributes_Code("Desc")(0).Translation.text(English) & "<br/>"
                End If
            Else
                stringPsu = stringPsu & psuUnit.SKU & "<br/>"
            End If


        Next
        Literal1.Text = "<br/>" & stringPsu

    End Sub

    Protected Sub btnFixOS_Click(sender As Object, e As EventArgs) Handles btnFixOS.Click
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)
        OSAutoAdd.fixAutoAdds(lid)
    End Sub



    Protected Sub btnCpkReport_Click(sender As Object, e As EventArgs) Handles btnCpkReport.Click
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)
        Dim sysList As List(Of clsSysCarePack) = OSAutoAdd.CarePackReports(lid)
        Response.Clear()
        Response.Buffer = True
        Response.AddHeader("content-disposition", _
                "attachment;filename=CarePAckAutoAddReport.csv")
        Response.Charset = ""
        Response.ContentType = "application/text"

        Dim sb As New StringBuilder()
        'For k As Integer = 0 To sysList.
        '    'add separator
        '    sb.Append(dt.Columns(k).ColumnName + ","c)
        'Next
        sb.Append("System SKU, System Desc, AutoAdd Carepack Sku, Carepack Desc")
        'append new line
        sb.Append(vbCr & vbLf)
        For Each sys In sysList
            'add separator
            sb.Append(sys.sysSkus & "," & sys.sysDesc & "," & sys.carepackSku & "," & sys.carePackDesc)
            'append new line
            sb.Append(vbNewLine)
        Next
        Response.Output.Write(sb.ToString())
        Response.Flush()
        Response.End()
    End Sub
    Protected Sub cmdFixCarePack_Click(sender As Object, e As EventArgs) Handles cmdFixCarePack.Click
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)
        OSAutoAdd.FixCarePacks(lid)
    End Sub

    Protected Sub btnManufacturer_Click(sender As Object, e As EventArgs) Handles btnManufacturer.Click

        Import.Manufacturer()

    End Sub

    Protected Sub Button42_Click(sender As Object, e As EventArgs) Handles Button42.Click
        fixTranslations()


    End Sub

    Protected Sub Button43_Click(sender As Object, e As EventArgs) Handles Button43.Click

        fixFilterDefaults()
    End Sub

    Protected Sub Button44_Click(sender As Object, e As EventArgs) Handles Button44.Click

        Dim sw As StreamWriter = New StreamWriter("c:\temp\mem.txt")
        For Each branch In iq.Branches.Values
            If branch.Product IsNot Nothing Then
                If branch.Product.isSystem Then
                    Dim product As clsProduct = branch.Product

                    Dim cpus As Integer = 0
                    Dim mem As Integer = 0
                    For Each slot In branch.slots.Values
                        If slot.Type.MajorCode = "CPU" Then
                            If slot.numSlots < 1 Or slot.numSlots > 4 Then Stop
                            For Each cb In branch.childBranches.Values
                                If branch.Translation.text(English).ToLower.Contains("chassis") Then
                                    For Each cs In cb.slots.Values
                                        If cs.Type.MajorCode = "MEM" Then
                                            Beep()
                                        End If
                                    Next
                                End If
                            Next
                        End If
                        cpus += slot.numSlots
                        If slot.Type.MajorCode = "MEM" Then mem += slot.numSlots
                    Next

                    If mem > 0 And cpus > 0 Then
                        sw.WriteLine(product.SKU & " " & product.i_Attributes_Code("desc")(0).Translation.text(English) & " has " & mem & " mem & " And " & cpus & " & cpus)
                    End If

                    'Return Me.Type.MajorCode & "_" & Me.Type.MinorCode & "_" & Me.path & "_" & Math.Sign(Me.numSlots) & "_" & Me.slotNum.sqlvalue

                End If
            End If
        Next

        sw.Close()

    End Sub

    Protected Sub Button45_Click(sender As Object, e As EventArgs) Handles Button45.Click
        Dim filename As String = MapPath("CarePackTablesv3test.xls")
        Dim connectionString As String = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties=Excel 12.0;", filename)
        Dim dtSheets As DataTable
        Using objConn As OleDbConnection = New OleDbConnection(connectionString)
            objConn.Open()
            dtSheets = objConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, _
            New Object() {Nothing, Nothing, Nothing, "TABLE"})
        End Using
        Dim ds As DataSet = New DataSet()
        For Each row In dtSheets.Rows
            Dim adapter As OleDbDataAdapter = New OleDbDataAdapter("SELECT * FROM [" & row("TABLE_NAME").ToString() & "]", connectionString)
            adapter.Fill(ds, row("TABLE_NAME").ToString())

        Next
        Dim dtServiceLevelsMap As DataTable = ds.Tables("ServiceLevelsMap$")
        Dim dtServiceType As DataTable = ds.Tables("ServiceType$")
        Dim dtResponse As DataTable = ds.Tables("Response$")
        Dim dtTROAA As DataTable = ds.Tables("TROAA$")
        Dim dtServiceOptions As DataTable = ds.Tables("ServiceOptions$")
        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        da.DBExecutesql(con, "Truncate Table TROAA")
        da.DBExecutesql(con, "DELETE FROM ServiceLevelMap")
        da.DBExecutesql(con, "DELETE FROM ServiceType")
        da.DBExecutesql(con, "DELETE FROM Response")
        da.DBExecutesql(con, "DBCC CHECKIDENT ('ServiceType',RESEED, 0)")
        da.DBExecutesql(con, "DBCC CHECKIDENT ('Response',RESEED, 0)")
        da.DBExecutesql(con, "DBCC CHECKIDENT ('ServiceLevelMap',RESEED, 0)")
        da.DBExecutesql(con, "DBCC CHECKIDENT ('TROAA',RESEED, 0)")

        Using adapterServiceType As SqlDataAdapter = New SqlDataAdapter("select * from ServiceType", con)
            Dim dtiqServiceType As DataTable = New DataTable()
            adapterServiceType.Fill(dtiqServiceType)
            For Each row In dtServiceType.Rows
                Dim rowIQ As DataRow = dtiqServiceType.NewRow()
                If row("DisplayOrder") IsNot Nothing Then
                    Dim order As Integer = Convert.ToInt32(row("DisplayOrder"))
                    rowIQ("mfrCode") = row("Mfr_Code")
                    If row("Title") IsNot Nothing Then rowIQ("FK_Translation_Key_Title") = iq.AddTranslation(row("Title"), English, "CPQ", order, Nothing, 0, False).Key
                    If row("LongDescription") IsNot Nothing And Not IsDBNull(row("LongDescription")) Then rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("LongDescription"), English, "CPQ", order, Nothing, 0, False).Key
                    rowIQ("serviceTypeDefault") = Convert.ToBoolean(row("Default"))
                    rowIQ("HPID") = row("ID")
                    dtiqServiceType.Rows.Add(rowIQ)
                End If
            Next
            Dim ESCBuilder As SqlCommandBuilder = New SqlCommandBuilder(adapterServiceType)
            adapterServiceType.UpdateCommand = ESCBuilder.GetUpdateCommand()
            adapterServiceType.Update(dtiqServiceType)


        End Using
        Using adapterResponse As SqlDataAdapter = New SqlDataAdapter("select * from Response", con)
            Dim dtiqResponse As DataTable = New DataTable()
            adapterResponse.Fill(dtiqResponse)
            For Each row In dtResponse.Rows
                Dim rowIQ As DataRow = dtiqResponse.NewRow()
                If row("DisplayOrder") IsNot Nothing Then
                    Dim order As Integer = Convert.ToInt32(row("DisplayOrder"))
                    rowIQ("mfrCode") = row("Mfr_Code")
                    If row("Title") IsNot Nothing Then rowIQ("FK_Translation_Key_Title") = iq.AddTranslation(row("Title"), English, "CPQ", order, Nothing, 0, False).Key
                    If row("LongDescription") IsNot Nothing And Not IsDBNull(row("LongDescription")) Then rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("LongDescription"), English, "CPQ", order, Nothing, 0, False).Key
                    rowIQ("ResponseDefault") = Convert.ToBoolean(row("Default"))
                    rowIQ("HPID") = row("ID")
                    dtiqResponse.Rows.Add(rowIQ)
                End If
            Next
            Dim ESCBuilder As SqlCommandBuilder = New SqlCommandBuilder(adapterResponse)
            adapterResponse.UpdateCommand = ESCBuilder.GetUpdateCommand()
            adapterResponse.Update(dtiqResponse)


        End Using

        Using adapterServiceLevelMap As SqlDataAdapter = New SqlDataAdapter("select * from ServiceLevelMap", con)
            Dim dtiqServiceType As DataTable = New DataTable()
            Dim dtiqResponse As DataTable = New DataTable()
            Using adapterServiceType As SqlDataAdapter = New SqlDataAdapter("select * from ServiceType", con)
                adapterServiceType.Fill(dtiqServiceType)
            End Using
            Using adapterResponse As SqlDataAdapter = New SqlDataAdapter("select * from Response", con)
                adapterResponse.Fill(dtiqResponse)
            End Using

            Dim dtiqServiceLevelMap As DataTable = New DataTable()
            adapterServiceLevelMap.Fill(dtiqServiceLevelMap)
            For Each row In dtServiceLevelsMap.Rows
                Dim rowIQ As DataRow = dtiqServiceLevelMap.NewRow()
                If row("Mfr_Code") IsNot Nothing Then

                    rowIQ("mfrCode") = row("Mfr_Code")
                    rowIQ("ServiceLevel") = Convert.ToInt32(row("ServiceLevel"))
                    rowIQ("ServiceLevelGroup") = row("ServiceLevelGroup")
                    rowIQ("SuperGroup") = row("SuperGroup")
                    If row("Description") IsNot Nothing And Not IsDBNull(row("Description")) Then rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("Description"), English, "CPQ", 1, Nothing, 0, False).Key

                    rowIQ("Duration") = Convert.ToInt32(row("Duration"))

                    If row("WarrantyType") = "S" Then rowIQ("PostWarranty") = False Else rowIQ("PostWarranty") = True
                    rowIQ("Disabled") = Convert.ToBoolean(row("Supress"))

                    If row("Fk_ServiceType_ID") IsNot Nothing And Not IsDBNull(row("Fk_ServiceType_ID")) Then
                        Dim foundrows() As DataRow
                        foundrows = dtiqServiceType.Select("HPID = '" & Trim(row("Fk_ServiceType_ID")) & "'")
                        If foundrows.Count = 1 Then
                            Dim selectedRow As DataRow = foundrows(0)
                            rowIQ("Fk_ServiceType_ID") = Convert.ToInt32(selectedRow("ID"))
                        End If
                    End If

                    If row("Fk_Response_ID") IsNot Nothing And Not IsDBNull(row("Fk_Response_ID")) Then
                        Dim foundrows() As DataRow
                        foundrows = dtiqResponse.Select("HPID = '" & Trim(row("Fk_Response_ID")) & "'")
                        If foundrows.Count = 1 Then
                            Dim selectedRow As DataRow = foundrows(0)
                            rowIQ("Fk_Response_ID") = Convert.ToInt32(selectedRow("ID"))
                        End If
                    End If

                    If row("HPE_DMR") IsNot Nothing And Not IsDBNull(row("HPE_DMR")) Then rowIQ("hpeDMR") = Convert.ToBoolean(row("HPE_DMR"))
                    If row("HPE_CDMR") IsNot Nothing And Not IsDBNull(row("HPE_CDMR")) Then rowIQ("hpeCDMR") = Convert.ToBoolean(row("HPE_CDMR"))
                    If row("HPI_ADP") IsNot Nothing And Not IsDBNull(row("HPI_ADP")) Then rowIQ("hpiADP") = Convert.ToBoolean(row("HPI_ADP"))
                    If row("HPI_DMR") IsNot Nothing And Not IsDBNull(row("HPI_DMR")) Then rowIQ("hpiDMR") = Convert.ToBoolean(row("HPI_DMR"))
                    If row("HPI_Travel") IsNot Nothing And Not IsDBNull(row("HPI_Travel")) Then rowIQ("hpiTravel") = Convert.ToBoolean(row("HPI_Travel"))
                    If row("HPI_Traceing") IsNot Nothing And Not IsDBNull(row("HPI_Traceing")) Then rowIQ("hpiTracing") = Convert.ToBoolean(row("HPI_Traceing"))
                    If row("HPI_Theft") IsNot Nothing And Not IsDBNull(row("HPI_Theft")) Then rowIQ("hpiTheft") = Convert.ToBoolean(row("HPI_Theft"))

                    dtiqServiceLevelMap.Rows.Add(rowIQ)
                End If
            Next
            Dim ESCBuilder As SqlCommandBuilder = New SqlCommandBuilder(adapterServiceLevelMap)
            adapterServiceLevelMap.UpdateCommand = ESCBuilder.GetUpdateCommand()
            adapterServiceLevelMap.Update(dtiqServiceLevelMap)



        End Using

        Using adapterTROAA As SqlDataAdapter = New SqlDataAdapter("select * from TROAA", con)
            Dim dtiqServiceLevelMap As DataTable = New DataTable()
            Using adapterServiceLevelMap As SqlDataAdapter = New SqlDataAdapter("select * from ServiceLevelMap", con)
                adapterServiceLevelMap.Fill(dtiqServiceLevelMap)
            End Using
            Dim dtiqTROAA As DataTable = New DataTable()
            adapterTROAA.Fill(dtiqTROAA)
            For Each row In dtTROAA.Rows
                Dim rowIQ As DataRow = dtiqTROAA.NewRow()
                If row("DisplayOrder") IsNot Nothing Then
                    Dim order As Integer = Convert.ToInt32(row("DisplayOrder"))
                    rowIQ("SysFamily") = row("SysFamily")
                    rowIQ("SlotTypeCode") = Convert.ToInt32(row("SlotTypeCode"))
                    rowIQ("ServiceLevel") = Convert.ToInt32(row("ServiceLevel"))
                    rowIQ("DisplayOrder") = Convert.ToInt32(row("DisplayOrder"))
                    Dim foundrows() As DataRow
                    foundrows = dtiqServiceLevelMap.Select("ServiceLevel = " & Trim(row("ServiceLevel")))
                    If foundrows.Count = 1 Then
                        Dim selectedRow As DataRow = foundrows(0)
                        rowIQ("FK_ServiceLevelMap_ID") = Convert.ToInt32(selectedRow("ID"))
                    End If
                    If iq.i_region_code.ContainsKey(row("Region")) Then
                        rowIQ("FK_Region_ID") = iq.i_region_code(row("Region")).ID
                    End If

                    dtiqTROAA.Rows.Add(rowIQ)
                End If
            Next
            Dim ESCBuilder As SqlCommandBuilder = New SqlCommandBuilder(adapterTROAA)
            adapterTROAA.UpdateCommand = ESCBuilder.GetUpdateCommand()
            adapterTROAA.Update(dtiqTROAA)


        End Using


    End Sub

    Protected Sub Button46_Click(sender As Object, e As EventArgs) Handles Button46.Click


        Import.sweepFIos()


    End Sub

    Protected Sub Button47_Click(sender As Object, e As EventArgs) Handles Button47.Click

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Import.FIOs(con)

        con.Close()

    End Sub

    Protected Sub Button48_Click(sender As Object, e As EventArgs) Handles Button48.Click
        Import.TestFastFind()


    End Sub

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub

    Protected Sub BtnOptionsPerSystem_Click(sender As Object, e As EventArgs) Handles BtnOptionsPerSystem.Click
        Utility.OptionsPerSystem()
    End Sub


    Protected Sub CheckFamilies_Click(sender As Object, e As EventArgs) Handles CheckFamilies.Click
        Import.checkfamilies()

    End Sub

    Private Sub FixMSBranches() Handles ButtonFixMicrosoft.Click

        Dim errorMessages As List(Of String) = New List(Of String)

        For Each branch In iq.Branches.Values

            ' Find each "Microsoft" branch we need to fix
            If branch.Translation.text(English) = "Microsoft" AndAlso branch.childBranches.Count > 0 Then

                Dim microsoftBranch = branch

                ' Look for a sibling "Microsoft OS" branch
                Dim microsoftOsBranch As clsBranch = microsoftBranch.Parent.FindBranchByNameBelow("Microsoft OS", "", True, 12)
                If microsoftOsBranch Is Nothing Then Continue For

                ' Look for the "Operating System" and "Client & Device Licencing" branches under the "Microsoft" branch
                Dim osBranch As clsBranch = Nothing
                Dim calBranch As clsBranch = Nothing
                For Each b In microsoftBranch.childBranches.Values

                    If b.Translation.text(English) = "Operating System" Or b.Translation.text(English) = "Operating Systems" Then
                        osBranch = b
                    End If

                    If b.Translation.text(English) = "Client & Device Licencing" Then
                        calBranch = b
                    End If

                Next

                If osBranch IsNot Nothing AndAlso calBranch IsNot Nothing Then

                    FixMSSubbranch(osBranch, calBranch, errorMessages, microsoftOsBranch, "sof1")
                    FixMSSubbranch(calBranch, osBranch, errorMessages, microsoftOsBranch, "sof4")

                End If

            End If

        Next

    End Sub

    Private Sub FixMSSubbranch(targetBranch As clsBranch, siblingTargetBranch As clsBranch, errorMessages As List(Of String), microsoftOsBranch As clsBranch, sof As String)

        ' targetBranch is either the "Operating System" or the "Client & Device Licencing" subbranch of the Software/Microsoft branch - 
        ' this routine moves it to Software/Microsoft OS and populates it from the data held directly under Software/Microsoft OS

        ' Give the branch its new parent
        targetBranch.Parent = microsoftOsBranch
        targetBranch.Update(errorMessages)

        ' Delete any existing children - we'll take the list under the "Microsoft OS" branch as more reliable
        For Each b In targetBranch.childBranches.Values

            b.deleted = True
            b.Update(errorMessages)

        Next

        ' Move each sof1/sof4 product from directly under the "Microsoft OS" branch to the target subbranch
        For Each b In microsoftOsBranch.childBranches.Values

            If Not b.ID = targetBranch.ID AndAlso Not b.ID = siblingTargetBranch.ID Then

                If b.Product IsNot Nothing Then

                    If String.Equals(b.Product.ProductType.Code, sof, StringComparison.InvariantCultureIgnoreCase) Then

                        b.Parent = targetBranch
                        b.Update(errorMessages)

                    End If

                End If

            End If

        Next

    End Sub

    Protected Sub Button49_Click(sender As Object, e As EventArgs) Handles Button49.Click
        For Each branch In iq.Branches.Values
            If branch.Product IsNot Nothing AndAlso branch.Product.isSystem Then

                If branch.slots.Values.Where(Function(sl) sl.Type.MajorCode.ToUpper = "WTY").Count = 0 Then
                    Dim slt = New clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), branch, "", 3, Nothing, New NullableInt(), 0, 0)
                End If
            End If

        Next
    End Sub
    Protected Sub btnCullBadOptions_Click(sender As Object, e As EventArgs) Handles btnCullBadOptions.Click

        Import.CullBadOptions()


    End Sub

    Protected Sub btnFixQuantities_Click(sender As Object, e As EventArgs) Handles btnFixQuantities.Click

        Dim todel As New HashSet(Of String)
        Dim DC As Integer = 0
        Dim sysSKU As String = txtSysSku.Text
        '  If sysSKU <> "" Then

        Dim dicfios As New Dictionary(Of String, Dictionary(Of String, Integer))

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        dicfios = Import.FIOs(con, "", Nothing) '"'" & sysSKU & "'")

        Dim pth$ = ""
        'Dim systembranch As clsBranch = RootBranch.findChildBySKU2("tree.1", sysSKU, pth, False)

        Dim sysLocs As Dictionary(Of String, clsBranch) = RootBranch.findSystemBranches("tree.1")

        For Each Path In sysLocs.Keys

            Dim qdic As New Dictionary(Of String, HashSet(Of clsQuantity))
            Dim systembranch As clsBranch = sysLocs(Path)

            ' If systembranch.Product.SKU = "788096-425" Then Stop

            systembranch.descendantQuantities(qdic)

            '  If qdic.ContainsKey("EJ013B") Then
            ' Dim a = 0
            ' End If

            For Each optionsku In qdic.Keys
                If optionsku <> systembranch.Product.SKU Then
                    If iq.i_SKU(optionsku).i_Attributes_Code.ContainsKey("opttype") Then
                        Dim ot As String = iq.i_SKU(optionsku).i_Attributes_Code("opttype")(0).Translation.text(English).ToUpper
                        If ot = "TAP" Then
                            If dicfios.ContainsKey(systembranch.Product.SKU) Then
                                If Not dicfios(systembranch.Product.SKU).ContainsKey(optionsku) Then
                                    Dim qc As Integer = qdic(optionsku).Count
                                    For Each q In qdic(optionsku)
                                        Dim dbg$ = "removed " & PathName(Path)
                                        todel.Add(q.ID)
                                        q.deleted = True
                                        DC += 1
                                    Next
                                End If
                            Else
                                '        Stop
                            End If
                        End If
                    End If
                End If
            Next
        Next
        con.Close()

        da.DBExecutesql("UPDATE quantity SET deleted=1 WHERE id in ('" & Join(todel.ToArray, "','") & "');")

        Debug.Print(DC)
        ' End If
    End Sub

    Protected Sub btnFixMissingMemory_Click(sender As Object, e As EventArgs) Handles btnFixMissingMemory.Click
        Import.FixMissingMemory()
    End Sub

    Protected Sub Button50_Click(sender As Object, e As EventArgs) Handles Button50.Click
        Import.checkoptions()
    End Sub

    Protected Sub btnDeleteCarepacks_Click(sender As Object, e As EventArgs) Handles btnDeleteCarepacks.Click
        CarePackModule.DeleteAllCarePacks()
    End Sub

    Protected Sub cmdAddAll_Click(sender As Object, e As EventArgs) Handles cmdAddAll.Click
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)
        CarePackModule.AddAllCarePacks(lid)
    End Sub
End Class
