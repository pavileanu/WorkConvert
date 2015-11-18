
Option Strict On
Option Explicit On
Imports IQ.clsBranchState 'Allows 
Imports System.IO
Imports System.Xml

Public Class showbranch
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'If iq Is Nothing Then Response.Redirect("signin.aspx")

        'This is ASPX is called via Ajax/js getBranches 
        'request paramaters may include CMD,Path,Into

        'Generally a call this this page is going to perform some manipulation (as determined by the CMD)
        '.. and then spit out a set of branches - which will replace the content of the DIV at 'Path'
        'Often (for a simple 'open a branch') that content is the branch being opened - and its children.

        If Not clsIQ.IsLoaded Then Exit Sub

        If Request.QueryString("lid") Is Nothing Then Throw New Exception("ShowChildren was called without an LID ! - querystring was '" & Request.RawUrl & "'")


        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))
        If lid = 0 Then Throw New Exception("LID evaluates to 0 - querystring was '" & Request.RawUrl & "'")


        ' This is an ajax call so redirect IT to the login screen is a terrible idea!
        'If iq.SeshAlive(lid) = False Then Response.Redirect("signin.aspx", True) 

        'Dim cmd As String = Request("cmd")
        'Dim path As String = Request("path")
        Dim Treewidth As Single = CSng(Request("treewidth"))
        Dim emConversion As Single = CSng(Request("emPixel"))

        If Request("Paradigm") <> "" Then  'happens if they cick a a system (or breadcrumb) in systems search
            Select Case UCase(Request("Paradigm"))
                Case Is = "B"
                    iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem  'Browse Mode
                    iq.sesh(lid, "showOnly") = Nothing
                    ClearBranchStates(lid)
                Case Is = "C"
                    iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem 'configure
                Case Else
                    Beep()
            End Select
        End If

        If Request("showOnly") <> "" Then
            iq.sesh(lid, "showOnly") = Request("showOnly")
        Else
            iq.sesh(lid, "showOnly") = 0
        End If

        If Request("to") <> "" Then
            ClearBranchStates(lid)
        End If

        Dim Paradigm As enumParadigm = CType(iq.sesh(lid, "Paradigm"), enumParadigm)

        Try
            Debug.Print(Request.RawUrl & "xx")
        Catch ex As System.Exception
            If ex.Message.Contains("A potentially dangerous Request.RawUrl value") Then
                errorMessages.Add("Tags are not permitted on this screen.")
            End If
        End Try

        'Dim pth As String = If(Request("filterPath") IsNot Nothing, Request("filterPath").ToString(), Request("path")) 'Path generally equates to DivToFill - so for squares is often the root path (tree.1)
        Dim pth As String = Request("path") 'Path generally equates to DivToFill - so for squares is often the root path (tree.1)
        If pth = "" Then
            pth = CStr(iq.sesh(lid, "path"))
        Else
            iq.sesh(lid, "path") = pth ' pth 'tree.aspx will render from here in future (and if refreshed)
        End If

        Dim bi As clsBranchInfo = New clsBranchInfo(lid, pth, Nothing, Treewidth, Paradigm, errorMessages)  'Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
        If Request("Promolink") IsNot Nothing Then
            iq.sesh(lid, "promoinforce") = iq.Promos(CInt(Request("promolink"))).Id
        ElseIf pth = "tree.1" And Request("into") = "tree" AndAlso iq.seshDic(lid).ContainsKey("refreshing") AndAlso Not CType(iq.sesh(lid, "refreshing"), Boolean) Then
            If iq.seshDic(lid).ContainsKey("promoinforce") Then iq.seshDic(lid).Remove("promoinforce")
            '   Dim sh = iq.seshTyped(Of Dictionary(Of String, clsScreenHeader))(lid, "screenHeaders")
            '     If sh IsNot Nothing AndAlso sh.ContainsKey("tree.1") Then
            bi.InvalidateMatrixBelow("tree.1")
            'End If
        End If

        If lid = 0 Then
            errorMessages.Add("* Session ID was 0")
        ElseIf iq.sesh(lid, "BuyerAccount") Is Nothing Then
            errorMessages.Add("Sorry, Your session has been reset - please log in again")
        End If

        Dim cursorpath As String = ""

        '     If Request("path") = "" Then Stop
        Dim EndPath As String = String.Empty
        Dim cmd As String = Request("cmd")
        If errorMessages.Count = 0 Then

            'The clsBranchInfo class encapsulates alot of parameters that need to be passed forward (into 
            'ProcessCommand returns information about the branch to render from

            Dim bs As clsBranchState = getbranchstate(bi.lid, bi.path)
            Dim pbs As clsBranchState = getBranchStateAbove(lid, bi.path, errorMessages)  'we may be looking at united branches - their (direct) parent may never have been opened and has no state

            '   If pbs Is Nothing Then Stop

            'processcommand - returns the 'new' cursor path 
            'bi.divtofill is populated by processCommand (often to 'Path', sometimes (for OpenFrom's) to "tree"

            cursorpath = ProcessCommand(bi, bs, pbs, errorMessages) 'NB: Both BI as BS can be manipulated - as some commands require us to render the tree from the root

            iq.sesh(bi.lid, "treeCursorPath") = cursorpath
            If bi.path = cursorpath Then bi.isTreeCursor = True

            Form.Controls.Add(NewLit("!DivToFill:" & bi.divToFill)) '<tells JS showBranches() which Div to replace

            Form.Controls.Add(NewLit("!BeginBranches")) '<start of content marker
            If (cmd = "shoppingList" Or cmd = "optionsPriceList") And errorMessages.Count > 0 Then
                Dim errorlist As String = ""
                For Each errMsg In errorMessages
                    errorlist = errorlist & errMsg & "|"
                Next
                errorlist = "!ToolsError" & errorlist & "!EndToolsError"
                Form.Controls.Add(NewLit(errorlist))
            Else
                OutputErrors(Form.Controls, errorMessages, lid) 'OUTPUT pre branch (post command) errors
            End If

            '  errorMessages.Clear()

            'this recurses for any children that are open - rendering a (potentially large) segment of the tree
            '    Form.Controls.Add(ErrorDymo(Treewidth.ToString))


            Dim pnl As Panel = bi.branch.UI(bi, EndPath, errorMessages)  '<<<HERE'S WHERE THE MAIN OUPUT IS GENERATED

            If bi.divToFill = "tree" Then
                Dim tp As Panel = New Panel
                tp.ID = "tree"

                tp.Controls.Add(pnl)

                Dim bep As Panel = New Panel
                bep.CssClass = "basketErrors"

                'these are shoppling list exceptions/warnings
                tp.Controls.Add(bep)
                bep.Controls.Add(outputMessages(bi.userMessages))

                OutputErrors(tp.Controls, errorMessages, lid) 'OUTPUT pre branch (post command) errors

                Form.Controls.Add(tp)
            Else
                Form.Controls.Add(pnl) 'NB: the branch UI may include error messages
                OutputErrors(Form.Controls, errorMessages, lid) 'OUTPUT pre branch (post command) errors
                'these are shoppling list exceptions/warnings
                Form.Controls.Add(outputMessages(bi.userMessages))
            End If

        Else
            'Form.Controls.Add(NewLit("!DivToFill:" & bi.path)) '<tell JS showBranches() which Div to replace
            Form.Controls.Add(NewLit("!DivToFill:tree")) '<tell JS showBranches() which Div to replace
            Form.Controls.Add(NewLit("!BeginBranches")) '<start of content marker
            OutputErrors(Form.Controls, errorMessages, lid, True) 'OUTPUT ANY pre branch (post command) errors

        End If

        'The oputput of this ASPX goes (as a result of the callback specified on the rExec in the JS getBranches()) 
        'to the JS ShowBranches Function... 
        Form.Controls.Add(NewLit("!EndBranches"))  'End of content marker
        Form.Controls.Add(NewLit("!BreadCrumbs"))
        If Paradigm <> CType(iq.sesh(lid, "Paradigm"), enumBt) Then Paradigm = CType(iq.sesh(lid, "Paradigm"), enumParadigm)
        If Paradigm = enumParadigm.AddingSystem Then
            Form.Controls.Add(NewLit("<span class='paradigmIndicator'>Browsing<span>"))
        ElseIf Paradigm = enumParadigm.configuringSystem Then
            Form.Controls.Add(NewLit("<span class='paradigmIndicator'>Configuring<span>"))
        ElseIf Paradigm = enumParadigm.errorNotSet Then
            Form.Controls.Add(NewLit("<span class='paradigmIndicator'>NOT SET<span>"))

        End If


      


        'MakeRoundButton("setwarehouse.png","Set the warehousetxtWarehouse.Items.Add("TST")

        If cursorpath <> "" Then
            Form.Controls.Add(clsBranch.Breadcrumbs(lid, If(EndPath <> String.Empty, EndPath, cursorpath$), CType(iq.sesh(lid, "AgentAccount"), clsAccount).Language, errorMessages))
        End If

        Form.Controls.Add(NewLit("!EndBreadcrumbs"))  'End of content marker

        'Pass path and screen to client
        Form.Controls.Add(NewLit("!BeginPath" + pth + "!EndPath"))

        ' HPE/HPI system messages - display only when we're at the top of the tree, and don't display if the
        ' user suppressed them (clicked the X) in this session
        Dim messageHtml = String.Empty
        Dim msgs = String.Empty
        Dim agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim suppressKey = String.Format("Suppress{0}SystemMessages", agentAccount.mfrCode)
        If (Not agentAccount.Manufacturer = Manufacturer.Unknown) AndAlso (pth = iq.sesh(lid, "Root").ToString()) AndAlso (iq.sesh(lid, suppressKey) Is Nothing) Then

            Dim key As String = String.Format("{0}SystemMessage", agentAccount.mfrCode.ToUpper())
            If iq.UserMessages.ContainsKey(key) Then
                For Each message As clsMessage In iq.UserMessages(key).Where(Function(m) (m.Enabled AndAlso m.ChannelID <= 1 AndAlso m.ValidFrom <= Today AndAlso m.ValidTo >= Today))
                    msgs &= String.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(agentAccount.Language)))
                Next
                If Not String.IsNullOrEmpty(msgs) Then messageHtml = String.Format("<div ID='systemMessage' ClientIDMode='Static' runat='server'>{0}<a id='closeButton' onclick=""burstBubble(event);HideSystemMessage();""></a></div>", msgs)
            End If

            ' Also display any channel-specific messages found
            key = "ChannelMessage"
            If iq.UserMessages.ContainsKey(key) Then
                For Each message As clsMessage In iq.UserMessages(key).Where(Function(m) (m.Enabled AndAlso m.ChannelID = agentAccount.SellerChannel.ID AndAlso m.ValidFrom <= Today AndAlso m.ValidTo >= Today))
                    msgs &= String.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(agentAccount.Language)))
                Next
                If Not String.IsNullOrEmpty(msgs) Then messageHtml = String.Format("<div ID='systemMessage' ClientIDMode='Static' runat='server'>{0}</div>", msgs)
            End If

        End If
        Form.Controls.Add(NewLit(String.Format("!BeginMessages{0}!EndMessages", messageHtml)))

        Form.Controls.Add(NewLit("!BeginBanner" + getAdverts(lid, EndPath) + "!EndBanner"))
        If Request("cmd") = "optionsPriceList" Then
            Form.Controls.Add(NewLit("!Beginexp" + cursorpath + "!Endexp"))

        End If
        If bi.EffectiveHeader IsNot Nothing Then Form.Controls.Add(NewLit("!BeginScreen" & bi.EffectiveHeader.screen.ID & "!EndScreen"))

        'Dim quoteHasItems As Boolean = False
        'If iq.SeshContains(lid, "QuoteID") Then
        '    Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        '    Dim quote As clsQuote = agentAccount.Quotes(CInt(iq.sesh(lid, "QuoteID")))
        '    If quote.RootItem.Children.Count > 0 Then quoteHasItems = True
        'End If

        'Form.Controls.Add(NewLit("!State"))
        'If quoteHasItems Then
        '    Form.Controls.Add(NewLit("35em"))
        'Else
        '    Form.Controls.Add(NewLit("1em"))
        'End If
        'Form.Controls.Add(NewLit("!End"))  'End of content marker

    End Sub

    Protected Sub SuppressSystemMessages(sender As Object, e As EventArgs)
        Stop
    End Sub


    ''' <summary>Performs some manipulation generally on the state of the branch at path - based on the CMD. </summary>
    ''' <param name="cmd">'branch','close',switch to 'squares' (etc.)</param>
    ''' <returns>A clsBranchInfo object saying what to render</returns>
    ''' <remarks>Generally alters sesh variables which affect the subsequent appearance of the tree.</remarks>
    ''' returns the 'TreeCursortPath'
    Private Function ProcessCommand(ByRef bi As clsBranchInfo, ByRef branchState As clsBranchState, pbs As clsBranchState, ByRef errormessages As List(Of String)) As String

        'Dim treeCursorPath As String = CType(iq.sesh(bi.lid, "treeCursorPath"), String)

        'Dim msgs As List(Of String) = New List(Of String) 'We'll populate this list with any (shopping list/swift) errors
        'Dim parentpath = oneAbove(bi.path)
        ' Dim p() As String

        Dim cmd As String

        ' p = Split(cmd, "=")
        cmd = Request("cmd")  'p(0)
        If InStr(cmd, "=") <> 0 Then Stop

        ProcessCommand = bi.path 'return the path (to become the treecursor)

        bi.divToFill = ProcessCommand
        If Request("into") <> "" Then
            bi.divToFill = Request("into")
            'If bi.divToFill <> "tree" Then Stop ML - Removed
        End If

        If cmd.Length > 0 Then
            iq.sesh(bi.lid, "previouscommand") = cmd
        Else
            cmd = iq.sesh(bi.lid, "previouscommand").ToString()
        End If

        Dim branch As clsBranch = iq.Branches(CInt(Split(ProcessCommand, ".").Last))

        'Some (but not all) commands manipulate the descendants - so only get them if necessary (it's relatively expensive)
        Dim descendants As Dictionary(Of clsBranch, clsVisibility) = Nothing

        Dim mhs As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))

        Select Case cmd

            Case Is = "setWarehouse"
                'BRAZIL
                bi.buyerAccount.wareHouseFilter = Request("warehouse")

            Case Is = "quoteAll"

                Dim agentAccount As clsAccount = CType(iq.sesh(bi.lid, "agentAccount"), clsAccount)
                Dim buyerAccount As clsAccount = CType(iq.sesh(bi.lid, "buyerAccount"), clsAccount)

                Dim quoteID As Integer = CInt(iq.sesh(bi.lid, "quoteid"))
                Dim quote As clsQuote
                If quoteID = 0 Then
                    Dim NullPrice As NullablePrice = New NullablePrice(buyerAccount.Currency)
                    quote = New clsQuote(buyerAccount, agentAccount, Nothing, Now, Now, CInt(1), iq.i_state_GroupCode("QT-#NW"), NullPrice, buyerAccount.Currency, False, False, False, String.Empty, New nullableString(), New nullableString(), 0)
                    iq.sesh(bi.lid, "QuoteID") = quote.ID
                Else
                    quote = agentAccount.Quotes(quoteID)
                End If

                Dim results As New List(Of String)
                branch.QuoteAllSystemsBelow(bi.lid, bi.path, quote, errormessages, results)

                branch.message = "<PRE>" & Join(results.ToArray, vbCrLf) & "</PRE>"

            Case Is = "expandpanel"
                Dim ky$ = Request("key")
                iq.sesh(bi.lid, ky) = "x"

            Case Is = "collapsepanel"
                Dim ky$ = Request("key")
                iq.seshDic(bi.lid).Remove(ky)

            Case Is = "adopt" '(reparent - many branches )

                wipeCachedDataView(bi.path, bi.lid)
                Dim newParent As clsBranch = iq.Branches(CInt(Split(bi.path, ".".ToCharArray).Last))

                'the JS compiles a list of sources from the checked branches (their paths)
                Dim sourcebranch As clsBranch
                For Each s In Request("sources").Split(",".ToCharArray)
                    If s <> "" Then 'the JS untidily leaves an extra comma - but it's easier to deal with here 
                        sourcebranch = iq.Branches(CInt(Split(s, ".").Last))
                        sourcebranch.Parent.childBranches.Remove(sourcebranch.ID) 'otherwise we leave a copy behind !
                        sourcebranch.Parent = newParent
                        sourcebranch.Update(errormessages)

                        For Each slot In sourcebranch.slots.Values
                            If slot.path <> "" Then slot.path = bi.path & "." & sourcebranch.ID
                            slot.update(errormessages)
                        Next

                        For Each q In sourcebranch.slots.Values
                            If q.path <> "" Then q.path = bi.path & "." & sourcebranch.ID
                            q.update(errormessages)
                        Next
                    End If
                Next

                '     clsQuoteItem.replacepaths()

                bi.divToFill = "tree." & iq.RootBranch.ID.ToString
                bi.branch = iq.RootBranch
                bi.path = bi.divToFill

                'slots and quantities (on descendants) will need re pathing
                'any grafts and prunes in force will need manipulating
                'some quoteitems paths may be invalidated
            Case Is = "unprune"

                branch.Prunes(CInt(Request("id"))).delete()
                'branch.Prunes.Remove(CInt(Request("id"))) - the delete (above) does this

            Case Is = "snap" 'XMK serialize /snapshot

                Dim b As clsBranch = iq.Branches(CInt(bi.path.Split(".".ToCharArray).Last))


                'When called on a system we will 'cross SKus' (recurse the options)
                Dim crossSKUs As Boolean = False
                If b.Product IsNot Nothing AndAlso b.Product.isSystem Then crossSKUs = True

                Dim xmlw As XmlTextWriter = New XmlTextWriter("c:\temp\snap.xml", Encoding.UTF8)
                ''xmlw.WriteRaw(Replace("<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>", "'", Chr(34)))
                Dim pth = bi.path
                Dim em As New List(Of String)
                b.serializeRecursive(bi, 0, pth, xmlw, crossSKUs, errormessages)
                xmlw.Close()

                Utility.writeSystemsBelow(b)
                Utility.writeOptionsBelow(b)

            Case Is = "deleteVariant"
                Dim v As clsVariant = iq.Variants(CInt(Request("ID")))
                v.Delete(errormessages)

            Case Is = "deleteSlot"
                Dim s As clsSlot = branch.slots(CInt(Request("ID")))

                s.deleted = True
                s.update(errormessages)

                Dim newpath As String = bi.path
                Dim sysbranch As clsBranch = branch.FindSystemAbove(bi.path, newpath)

                bi = New clsBranchInfo(bi.lid, newpath)
                bi.divToFill = newpath
                bi.branch = sysbranch


            Case Is = "unDeleteSlot"
                Dim s As clsSlot = branch.slots(CInt(Request("ID")))

                s.deleted = False  'new 'soft' delete
                s.update(errormessages)

                Dim newpath As String = bi.path
                Dim sysbranch As clsBranch = branch.FindSystemAbove(bi.path, newpath)
                bi = New clsBranchInfo(bi.lid, newpath)
                bi.divToFill = newpath
                bi.branch = sysbranch

            Case Is = "deleteBranch"

                branch.deleted = True  'branch is determined by the last segement of request("path")
                branch.Update(errormessages)

            Case Is = "previewShredBranch"


                Dim counts As New Dictionary(Of String, Integer) 'total numbers of records by type affected
                Dim summary As String = ""
                branch.HardDelete(errormessages, summary, 0, False, counts)
                Dim tt As String = Xlt("Shred branch (completely destroys this branch and all its descendants and dependecies) - as above (you cannot undo !)", bi.agentAccount.Language)

                Dim btn As Literal = CoreCode.MakeRoundButton("shredBranch.png", tt, clsBranch.ButtonScript("cmd=shredBranch&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language)
                branch.message = "<div class='shredSummary'>" & summary & btn.Text & "&lt;--CAUTION </div>"  ' the <pre> tag tells the browser this is preformatted text, it will be rendered in fixed pitch with tabs, spaces and CRLF's preserved

            Case Is = "ShredBranch"

                Dim summary As String = ""
                Dim counts As New Dictionary(Of String, Integer) 'total numbers of records by type affected
                branch.HardDelete(errormessages, summary, 0, True, counts)
                branch.message = "<div class='Shredsummary'>" & summary & "</div>"


            Case Is = "unDeleteBranch"

                branch.deleted = False
                branch.Update(errormessages)

            Case Is = "deleteQuantity"
                Dim q As clsQuantity = iq.Quantities(CInt(Request("ID")))
                'c.delete(errormessages)
                q.deleted = True
                q.update(errormessages)

            Case Is = "unDeleteQuantity"
                Dim q As clsQuantity = iq.Quantities(CInt(Request("ID")))
                'c.delete(errormessages)
                q.deleted = False
                q.update(errormessages)

            Case Is = "deleteProductAttribute"
                Dim paid As Integer = CInt(Request("PAID"))
                Dim ProdId As Integer = CInt(Request("ID"))
                Dim product As clsProduct = iq.Products(ProdId)
                Dim pa As clsProductAttribute = product.Attributes(paid)
                pa.deleted = True
                pa.update(errormessages)

                'de-index - it to make it 'dissapear' from the UI (It will be 'really' gone next time the OM is loaded)
                product.i_Attributes_Code(pa.Attribute.Code).Remove(pa)
                If product.i_Attributes_Code(pa.Attribute.Code).Count = 0 Then
                    product.i_Attributes_Code.Remove(pa.Attribute.Code)
                End If


            Case Is = "unDeleteProductAttribute"
                Dim paid As Integer = CInt(Request("PAID"))
                Dim ProdId As Integer = CInt(Request("ID"))
                Dim product As clsProduct = iq.Products(ProdId)
                Dim pa As clsProductAttribute = product.Attributes(paid)

                're-index - it to make it 'reappear' in the UI
                If Not product.i_Attributes_Code.ContainsKey(pa.Attribute.Code) Then
                    product.i_Attributes_Code.Add(pa.Attribute.Code, New List(Of clsProductAttribute))
                End If
                product.i_Attributes_Code(pa.Attribute.Code).Add(pa)

                pa.deleted = False
                pa.update(errormessages)


            Case Is = "showQuickFilters"  'show (existing) quickfilters
                bi.setQuickFiltersVisible(True)
            Case Is = "hideQuickFilters"
                bi.setQuickFiltersVisible(False)
            Case Is = "shoppingList"
                ' set the branch to tree.1
                branch = iq.Branches(CInt(Split("tree.1", ".").Last))


                Dim agentAccount As clsAccount = CType(iq.sesh(bi.lid, "AgentAccount"), clsAccount)
                Dim shoppingListSku As String = Request("list")

                If (String.IsNullOrEmpty(shoppingListSku)) Then
                    'If (Not (errormessages.Contains("Please Enter An SKU (Some Text) In The Import Box."))) Then
                    errormessages.Add("Please enter a SKU (some text) in the Import box.")
                    'End If
                End If

                shoppingListSku = Replace(shoppingListSku, vbCrLf, vbCr)  'Switch all delimiters to CR's
                shoppingListSku = Replace(shoppingListSku, ";", vbCr)
                shoppingListSku = Replace(shoppingListSku, ",", vbCr)

                Dim p() As String = Split(shoppingListSku, vbCr)
                Dim systemProduct As clsProduct
                Dim checkedBranches As HashSet(Of clsBranch) = New HashSet(Of clsBranch)
                Dim productPath As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)
                Dim lastItem As Integer = p.Length - 1
                If p(lastItem).Contains("*") Then p(lastItem) = Split(p(lastItem), "*")(0)
                p(lastItem) = Trim(p(lastItem))

                While productPath.Count = 0
                    If iq.i_SKU.ContainsKey(p(lastItem)) Then
                        systemProduct = iq.i_SKU(p(lastItem))
                        If systemProduct.isSystem Then
                            productPath = branch.findProductBranches("tree.1", agentAccount.SellerChannel, systemProduct, False, checkedBranches, True)
                        End If
                    End If
                    lastItem = lastItem - 1
                    If lastItem < 0 Then Exit While
                    If p(lastItem).Contains("*") Then p(lastItem) = Split(p(lastItem), "*")(0)
                    p(lastItem) = Trim(p(lastItem))

                End While
                bi.userMessages = clsQuote.FromShoppingList(bi.lid, bi.agentAccount, bi.buyerAccount, Request("list"), errormessages) 'the usermessages are rendered out

                If bi.userMessages.Count = 0 And errormessages.Count = 0 Then

                    If productPath.Count > 0 Then
                        bi.branch = productPath.Values(0)

                        bi.path = productPath.Keys(0) 'this is the only place (other than the branchinfo constructor) that I set Path - Marting has (totally legitimately) switched it to readonly - but i need set it for shopping list and don't have the time for a bigger restructure NA
                    Else
                        bi.branch = branch
                    End If
                    Dim parentPathArray() As String = Split(bi.path, ".")
                    Dim parentpathreduced() As String = parentPathArray.Take(parentPathArray.Length - 1).ToArray()
                    Dim parentPAth As String = Join(parentpathreduced, ".")

                    bi.divToFill = "tree"
                    bi.Paradigm = enumParadigm.configuringSystem  'new (first level branches beneath systems were rendering as squares)
                    branchState = New clsBranchState(bi.lid, parentPAth, enumBt.Branch, False, bi.rownum, 100)
                    iq.sesh(bi.lid, "Paradigm") = enumParadigm.configuringSystem

                Else
                    iq.sesh(bi.lid, "Paradigm") = enumParadigm.AddingSystem
                    '    bi = New clsBranchInfo(bi.lid, "tree.1", Nothing, bi.treeWidth, enumParadigm.AddingSystem, errormessages)
                    '  iq.sesh(bi.lid, "refreshing") = True
                    If bi.userMessages.Count > 0 Then
                        For Each msg In bi.userMessages
                            errormessages.Add(msg)
                        Next
                    End If
                End If
                Open(bi, "open", descendants)

            Case Is = "optionsPriceList"

                Dim systemSKU As String = Request("systemsku")

                iq.sesh(bi.lid, "systemSKU") = systemSKU
                iq.sesh(bi.lid, "toolsCSVExport") = True

                Dim systemBI As clsBranchInfo = bi

                Dim branchType As enumBt = branchState.rca
                Dim switchedBranchType As Boolean = False
                If Not branchState.rca = enumBt.gridrow Then
                    systemBI.switchTo(enumBt.gridrow, errormessages)
                    switchedBranchType = True
                End If

                Dim sysPath As String = FindSystemPath(bi, systemBI, systemSKU, errormessages)

                Dim invalidSku As Boolean = False
                Try
                    systemBI = New clsBranchInfo(bi.lid, sysPath, Nothing, bi.treeWidth, bi.Paradigm, errormessages)
                Catch ex As Exception
                    invalidSku = True
                End Try

                If (Not systemBI Is Nothing) AndAlso
                    (Not systemBI.branch Is Nothing) AndAlso
                    (Not systemBI.branch.Product Is Nothing) AndAlso
                    (systemBI.branch.Product.Manufacturer <> systemBI.agentAccount.Manufacturer) Then
                    invalidSku = True
                End If

                If invalidSku Then
                    errormessages.Add(Xlt(" Part number not recognised. Please enter a valid SKU for this manufacturer.", systemBI.agentAccount.Language))
                End If

                If errormessages.Count = 0 Then
                    ProcessCommand = bi.path
                    bi.divToFill = "tree"

                    Dim priceConfig As Integer = systemBI.buyerAccount.SellerChannel.priceConfig
                    Dim hideReasons = systemBI.branch.ReasonsForHide(systemBI.buyerAccount, systemBI.foci, sysPath, priceConfig, False, errormessages)

                    Dim sysBiVisibility As clsVisibility = New clsVisibility(systemBI.branch, sysPath, hideReasons)

                    descendants = systemBI.visibleChildren(errormessages, True, 0, 0, True, False, True)

                    If Not descendants.ContainsKey(systemBI.branch) Then

                        ' Add the system branch
                        descendants.Add(systemBI.branch, sysBiVisibility)

                        ' Sort by:
                        ' 1 - system branch to the top
                        ' 2 - product type
                        ' 3 - product
                        descendants = descendants.OrderBy(Function(x) If(x.Key Is systemBI.branch, 0, 1)) _
                                                .ThenBy(Function(x) x.Key.Product.ProductType.Code) _
                                                .ThenBy(Function(x) x.Key.order).ToDictionary(Function(x) x.Key, Function(y) y.Value)

                        Dim screenHeader As clsScreenHeader = New clsScreenHeader(bi, descendants, False)

                        screenHeader.screen = iq.i_screens_code("ExCSV")

                        screenHeader.exportCSV(systemBI.lid, descendants, systemBI.buyerAccount, systemBI.agentAccount.Language, systemBI.foci, errormessages, True, True)
                    Else
                        errormessages.Add(" Please enter a valid SKU.")
                    End If

                End If

                If switchedBranchType Then
                    bi.switchTo(branchType, errormessages)
                End If

                bi.divToFill = String.Empty

                Return String.Empty

            Case Is = "exportGrid"

                ''get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                descendants = bi.visibleChildren(errormessages, True, 0, 0, True, True)
                bi.EffectiveHeader.exportCSV(bi.lid, descendants, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, False, True)

            Case Is = "showProducts" 'Shows an (arbitrary) list of products (as a matrix)
                Dim lst As String = Request("list")
                If lst <> "" Then
                    Dim branchid As Integer = 0
                    'Msgs are 'error's which we *DO* neeed to espose

                    bi = New clsBranchInfo(bi.lid, "tree.1", bi.lblMatches, bi.treeWidth, bi.Paradigm, errormessages)
                    ' A showProducts (swift2) command looks likes this getbranches('tree.1','showProducts=ABC123,253728-B21,9284749')
                    bi.userMessages = ShowProducts(bi.lid, Request("list"), branchid, bi.treeWidth, errormessages)
                    'bi.path = "tree." & branchid  'We pass the temporary (negative) branchID back as the bi.path -  I admit - this isn't very pretty 
                    bi.branch = iq.Branches(branchid) 'this will be negative
                    bi.divToFill = "tree"

                    'invalidate any headers we may have
                    Dim pth$ = "tree." & branchid
                    Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
                    If matrixHeaders.ContainsKey(pth) Then matrixHeaders.Remove(pth)

                    Dim bs As clsBranchState = bi.open(errormessages, True)
                    bi.switchTo(enumBt.gridrow, errormessages)
                    bs.rca = enumBt.gridrow

                End If

            Case Is = "switchTo"

                Dim bt As enumBt = enumBt.errorNotSet  'to notset (in which case we will use the Branch.rca property - see bi.Open
                Dim typechar As String = Request("bt")
                bt = CType(BTchar.IndexOf(typechar), enumBt)
                bi.switchTo(bt, errormessages)
                'bi.InvalidateMatrixBelow(bi.path, True)

            Case Is = "promofilter"
                descendants = bi.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice

                bi.CreateMatrixHeader(descendants)
                'If Not mhs.ContainsKey(bi.path) Then
                '    bi.matrixHeader = New clsScreenHeader(bi, descendants, True, errormessages) 'this creates the clsmatrix header AND stores it in the users session
                'End If

                'See if the promo field is filterable??
                If bi.ScreenHeader.screen.i_field_property.ContainsKey("promos(" + Request("promoType") + ")") Then
                    'MH Removal bi.ScreenHeader.UpdateFilters(CType(bi.ScreenHeader.matrix.i_field_property("promos(" + Request("promoType") + ")").ID, String) + "|EQ|1")
                Else
                    'Do something, what??
                End If

                Open(bi, "open", descendants)

            Case Is = "open", "openTab" 'this sets how the descendants will be rendered - 'Read as - set (or change) the way you render your children - consolidate with view maybe
                If iq.sesh(bi.lid, "custContext") IsNot Nothing Then
                    Dim custContext As clsCustomerContext = New clsCustomerContext()
                    custContext = CType(iq.sesh(bi.lid, "custContext"), clsCustomerContext)
                    bi.buyerAccount.wareHouseFilter = custContext.WareHouse
                End If

                Open(bi, cmd, descendants)

            Case Is = "openFiltered"

                descendants = bi.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                bi.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
                Dim filterFieldNotFound As String = ""
                bi.ScreenHeader.SetDefaultFilterOn(filterFieldNotFound)
                bi.setQuickFiltersVisible(True)
                If filterFieldNotFound <> "" Then
                    Dim fld As clsField = (From f In bi.ScreenHeader.Filters.Keys Where f.ID = CInt(filterFieldNotFound)).FirstOrDefault
                    If fld IsNot Nothing Then
                        bi.ScreenHeader.Filters.Remove(fld)

                    End If
                End If
                descendants = bi.visibleChildren(errormessages, True, 0, 0, True, True)
                'If descendants.Count = 0 Then
                '    Dim strFailedFilters As String = bi.ScreenHeader.Vw.RowFilter
                '    Dim newFilterString As List(Of String) = New List(Of String)
                '    Dim filterString() As String = Split(strFailedFilters, "AND")

                '    Dim r As Random = New Random
                '    ' Get random numbers between 1 and 3.
                '    ' ... The values 1 and 2 are possible.
                '    d(r.Next(0, 3))
                '    Console.WriteLine(r.Next(0, 3))
                '    Console.WriteLine(r.Next(1, 3))
                'End If
                Open(bi, cmd, descendants)

            Case Is = "openSquare"
                CloseAbove(bi.lid, bi.path)
                Open(bi, cmd, descendants)

            Case Is = "close"
                bi.close(errormessages)
            Case Is = "unite" '(unite - view all descendant products)
                branchState.United = True
            Case Is = "divide" '(divide - view categories)
                branchState.United = False 'It's important we flatten *after* having found the first visible child.                

            Case Is = "sort" 'update the sort orders

                Dim pd As clsPriorityDirection
                Dim V$
                If Request("value") <> "" Then
                    V$ = Request("value")
                    pd = New clsPriorityDirection(V$)
                Else
                    pd = New clsPriorityDirection(iq.Fields(CInt(Request("colID"))), CInt(Request("priority")), CStr(Request("direction")))
                End If


                bi.ScreenHeader.UpdateSorts(pd)
                descendants = bi.visibleChildren(errormessages, True, 0, 0, True, True)


            Case Is = "removeSort"
                bi.ScreenHeader.RemoveSort(CInt(Request("priority")))

            Case Is = "clearFilter"

                bi.ScreenHeader.ClearFilter(Request("filterId"))
                bi.InvalidateMatrixBelow(bi.path, False)

            Case Is = "clearGroupFilter"

                bi.ScreenHeader.ClearGroupFilter(Request("filterId"))
                bi.InvalidateMatrixBelow(bi.path, False)

            Case Is = "changeFilter"

                '  If descendants Is Nothing Then Stop
                ' descendants = bi.visibleChildren(errormessages, True, 0, 0, True, True)  '

                bi.ScreenHeader.UpdateFilters(Request("filterParams"))

                bi.InvalidateMatrixBelow(bi.path, False)

            Case Is = "removeFilter" 'Removes ONE of the filters from an active matrix header
                Dim fp$ = Request("filterPath")
                Dim ru As String = Request.RawUrl

                mhs(fp$).RemoveFilter(Request("filterParams"), errormessages)
                bi.InvalidateMatrixBelow(fp$)

            Case Is = "removeFilters" 'removes ALL of the filters from an active header

                Dim fp$ = Request("filterPath")
                mhs(fp$).removeFilters()
                mhs(fp$).QuickFiltersVisible = False 'hide the (now empty) filters

                'MH Removal - do we need this now/rebuild every view/databatale 'below' this
                'For Each pth In mhs.Keys
                '    Dim cbi As clsBranchInfo = New clsBranchInfo(bi.lid, pth, Nothing, bi.treeWidth, bi.Paradigm, errormessages)
                '    descendants = cbi.visibleChildren(errormessages, True, 0, 0, True, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                '    mhs(pth).rebuild(cbi, descendants, errormessages)

                'Next
                Open(bi, "openSquare", descendants)
                'Dim bs As clsBranchState = bi.open(errormessages, True) '<<<THIS DOES THE BIZ (creates branchstate - including RCA)
                'bs.rca = enumBt.OpenSquare
                'Dim bs As clsBranchState = New clsBranchState(bi.lid, "tree.1", enumBt.OpenSquare, False, 0, 1000)
                'Beep()
                bi.InvalidateMatrixBelow(bi.path)

            Case Is = "maxrows"
                iq.sesh(bi.lid, "maxrows." & bi.path) = CInt(Request("rows"))
                branchState.maxChildren = CInt(Request("rows"))
                '  bi.path = "tree.1"
                '   bi.Branch = iq.Branches(1)

            Case Is = "expandColumn"
                bi.ScreenHeader.setColState(iq.Fields(CInt(Request("fieldid"))), enumColState.HardExpanded)
                bi.ScreenHeader.CollapseColumns(bi.treeWidth, errormessages)

            Case Is = "collapseColumn"
                bi.ScreenHeader.setColState(iq.Fields(CInt(Request("fieldid"))), enumColState.HardCollapsed)

            Case Is = "defFilterOn"
                Open(bi, cmd, descendants)

                Dim parentPath = Request("to").Replace("." & Split(Request("to"), ".").Last.ToString, "")
                Dim bi3 As clsBranchInfo = New clsBranchInfo(bi.lid, parentPath, Nothing, bi.treeWidth, bi.Paradigm, errormessages)  'Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)

                If bi3 IsNot Nothing Then ' AndAlso bi3.branch.rca.Contains("M") Then
                    'Dealing with an L3 (ROK) switch the parent and set up the filters on the children
                    bi3.switchTo(enumBt.helpMechoose, errormessages) ' Switch to special HMC view (if its available on the parent branch)
                    For Each child In bi3.branch.childBranches.Values
                        Dim bi_child As clsBranchInfo = New clsBranchInfo(bi.lid, parentPath & "." & child.ID.ToString, Nothing, bi.treeWidth, bi.Paradigm, errormessages)  'Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
                        descendants = bi_child.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                        bi_child.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
                        'bi_child.ScreenHeader.SetDefaultFilterOn()
                        Dim srt = bi_child.ScreenHeader.FieldResultSet.Where(Function(f) f.Key.propertyName.Contains("technology")).FirstOrDefault
                        If srt.Key IsNot Nothing Then
                            bi_child.ScreenHeader.UpdateSorts(New clsPriorityDirection(srt.Key, 1, "desc"))
                        End If

                        bi_child.setQuickFiltersVisible(True)
                    Next
                Else
                    Dim bi2 As clsBranchInfo = New clsBranchInfo(bi.lid, Request("to"), Nothing, bi.treeWidth, bi.Paradigm, errormessages)  'Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
                    descendants = bi2.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                    bi2.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
                    bi2.ScreenHeader.SetDefaultFilterOn()
                    bi2.setQuickFiltersVisible(True)
                End If

            Case Is = "quickFilter" 'makes (and shows) a set of quickfilters on this branch  switches to grid and showsQuickFilter (help me choose button)

                '   branchState.United = True
                'descendants = bi.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice

                'If Not mhs.ContainsKey(bi.path) Then
                '    bi.matrixHeader = New clsScreenHeader(bi, descendants, True, errormessages) 'this creates the clsmatrix header AND stores it in the users session
                'End If

                'We fetch all prices so filtering and sorting can work (except the datatable isn't updated ! ( ) - the re-sort button needs to to repopulate the datatable (from the OM)
                '         For Each b In descendants.Keys
                'b.Product.GetPrices(bi.BuyerAccount, bi.BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, True) 'call the webservice for ALL descendants (becuase we will want to sort by price)
                'Next

                bi.setQuickFiltersVisible(True)
            Case Is = "removePromoLink"
                If iq.seshDic(bi.lid).ContainsKey("promoinforce") Then iq.seshDic(bi.lid).Remove("promoinforce")
                'Need to wipe out the promo in force field defs
                'If iq.seshTyped(Of List(Of String))(bi.lid, "pathDataLoaded") IsNot Nothing Then iq.seshTyped(Of List(Of String))(bi.lid, "pathDataLoaded").Clear()

                Open(bi, "openSquare", descendants)
                bi.InvalidateMatrixBelow(bi.path)
                'CloseBelow(bi.lid, "tree.1")
                'iq.seshTyped(Of Dictionary(Of String, clsBranchState))(bi.lid, "branchStates").Clear()
                'PloughPath(bi.lid, bi.path, errormessages, bi.treeWidth, bi.Paradigm)

            Case Is = "lock"
                branch.locked = True
                branch.Update(errormessages)

            Case Is = "unlock"
                branch.locked = False
                branch.Update(errormessages)

            Case Is = ""
                'It's OK to have no CMD
                Beep()
            Case Else
                errormessages.Add("Unrecognised command " & cmd)
        End Select
        'ProcessCommand = "tree"
    End Function
    Sub Open(ByRef bi As clsBranchInfo, cmd As String, ByRef descendants As Dictionary(Of clsBranch, clsVisibility))
        '   Dim bsa As clsBranchState = getbranchstate(bi.lid, bi.path)
        '   If bsa IsNot Nothing Then
        ' descendants = bi.visibleChildren(errormessages, True, 0, 0, True, False)
        ' If descendants.Count = 0 Then Stop
        ' End If
        If getbranchstate(bi.lid, bi.path) Is Nothing And bi.branch.rca.StartsWith("D") Then 'D are 'detail' squares
            'First open, no switch, lets fill out the matrixheader - not sure this is great but it works and doesnt do anything unnescessary
            Dim sh As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
            If Not sh.ContainsKey(bi.path) Then
                If descendants Is Nothing Then descendants = bi.visibleChildren(errorMessages, True, 0, 0, True, True)
                If bi.ScreenHeader Is Nothing Then bi.CreateMatrixHeader(descendants, False)
                bi.ScreenHeader.QuickFiltersVisible = False
            End If
        End If
        If Not iq.SeshContains(bi.lid, "refreshing") OrElse iq.sesh(bi.lid, "refreshing") Is Nothing AndAlso Request("To") Is Nothing Then CloseBelow(bi.lid, bi.path)
        iq.sesh(bi.lid, "refreshing") = Nothing

        Dim bs As clsBranchState = Nothing

        If Request("to") <> "" Then
            Dim topath$ = Request("to")
            PloughPath(bi.lid, Request("to"), errorMessages, bi.treeWidth, bi.Paradigm)
            CloseAbove(bi.lid, topath) '*KW
        Else
            bs = bi.open(errorMessages, True) '<<<THIS DOES THE BIZ (creates branchstate - including RCA)
        End If

        If cmd = "openTab" Or cmd = "openFiltered" Then 'Opening a tab renders from (and into) its parent (to refresh the sibilings, one of which must must be deactivated)
            HideSiblings(bi.lid, bi.path)
            bi = New clsBranchInfo(bi.lid, Left$(bi.path, InStrRev(bi.path, ".") - 1), bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages)
            'bi.path = Left$(bi.path, InStrRev(bi.path, ".") - 1)
            bi.divToFill = bi.path 'Left$(bi.path, InStrRev(bi.path, ".") - 1)
            'bi.Branch = iq.Branches(CInt(Split(bi.path, ".").Last)) 'note the bi.path has already been trimmed
        End If

    End Sub

    



    Private Function FindSystemPath(ByRef originalbi As clsBranchInfo, ByRef systembi As clsBranchInfo, sku$, ByRef errorMessages As List(Of String)) As String

        'Displays All options for a specified system
        'return the path to the system

        Dim part As clsTranslation = iq.AddTranslation("Part", English, "UI", 0, Nothing, 0, False)
        Dim parts As clsTranslation = iq.AddTranslation("Parts", English, "UI", 0, Nothing, 0, False)

        Dim agentAccount As clsAccount = CType(iq.sesh(originalbi.lid, "AgentAccount"), clsAccount)

        Dim systemProduct As clsProduct
        Dim systemPath$ = ""

        Dim partno$
        sku$ = Replace(sku$, vbTab, "")  'remove any crap they might have pasted in
        sku$ = Replace(sku$, vbCrLf, "")
        sku$ = Replace(sku$, vbLf, "")

        partno$ = Trim$(sku$)
        If iq.i_SKU.ContainsKey(partno$) Then
            systemProduct = iq.i_SKU(Trim(sku$))
            Dim checkedBranches As HashSet(Of clsBranch) = New HashSet(Of clsBranch)

            Dim locations As Dictionary(Of String, clsBranch) = iq.Branches(1).findProductBranches("tree.1", agentAccount.SellerChannel, systemProduct, False, checkedBranches, True)

            systemPath = locations.Keys(0)
            Dim systemBranch As clsBranch = locations.Values(0)

            'we pass an additional parameter with the matrix command which is the 'real' path
            'simply' open the system branch (with a matrix)

            '  iq.sesh(bi.lid, "treeCursor") = systemPath
            '  CloseBelow(bi.lid, systemPath) '@@

            'Dim nbi As clsBranchInfo = New clsBranchInfo(bi.lid, systemPath, Nothing, bi.treeWidth, enumParadigm.configuringSystem, errorMessages)

            '          nbi.InvalidateMatrixBelow(bi.path, True)

            If systemPath IsNot Nothing Then

                'ML - added back in, path should NEVER be changed in branch info, its the key and has attached matrix headers which will be cached against the wrong path
                ''   systembi = New clsBranchInfo(originalbi.lid, systemPath, originalbi.lblMatches, originalbi.treeWidth, originalbi.Paradigm, errorMessages)

                'bi.path = systemPath
                'bi.branch = systemBranch

                ''systembi.ScreenHeader = Nothing
                ''systembi.switchTo(enumBt.gridrow, errorMessages) 'was gridrow
                ''systembi.open(errorMessages, False)

                'Dim bs As clsBranchState = nbi.open(errorMessages, True)

                '    bi.EffectiveHeader = matrixHeaderAbove(bi.lid, bi.path, errorMessages)
                '   Debug.Print(bi.EffectiveHeader.screen.code)

                'Dim descendants As dictionary(of clsbranch,clsvisibility) = bi.visibleChildren(errorMessages, True, 0, 0, False, False)
                'Dim mh As clsScreenHeader = New clsScreenHeader(bi, descendants, False, errorMessages) ' reference is held in the users Sesh (so this wont go out of scope)
            End If
        Else
            ' SK - a duplicate error message is already displayed by ProcessCommand
            'errorMessages.Add("Part number " & sku$ & " is not recognised.")
        End If

        Return systemPath

    End Function

    Private Sub ClearBranchStates(lid As UInt64)

        'Dim branchStates As Dictionary(Of String, clsBranchState)
        CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState)).Clear()
        'branchStates.Clear()

    End Sub

    ''' <summary>Displays stock and price an (arbitrary, flat, comma delimited) list of parts -  creates a branch and set of child branches with a neagtive IDs (which can be rendered as a matrix) </summary>
    Public Function ShowProducts(lid As UInt64, l$, ByRef branchid As Integer, treewidth As Single, ByRef errormessages As List(Of String)) As List(Of String)

        Dim errs As List(Of String) = New List(Of String)

        Dim part As clsTranslation = iq.AddTranslation("Part", English, "UI", 0, Nothing, 0, False)
        Dim parts As clsTranslation = iq.AddTranslation("Parts", English, "UI", 0, Nothing, 0, False)
        Dim mbid As Integer 'minumium branch ID (Some negative number)

        If iq.SeshContains(lid, "swiftStart") Then  'lowest (most negative) - we create a slew of temproary negative branches - starting with a placeholder one, and then a bunch of children
            TidySwiftBranches(lid) 'removes any temporary swift branches we've created before
        End If

        Dim J = From q In iq.Branches.Keys 'Use LINQ to find the lowest (most negative!) branch ID (this is OK for multiple uses as the brahes *are* enetered into the golbal collection - even though ther'ye never inserted into the database)
        mbid = J.Min()
        If mbid > 0 Then mbid = 0
        mbid -= 1 'start at one *less* than the current min
        iq.sesh(lid, "swiftStart") = mbid


        'create a floating branch (to hook all the products to)... it's not persisted (no insert is made to the database) because we use the constructor (sub new) with an ID parameter
        Dim HeaderBranch As New clsBranch(mbid, Nothing, Nothing, iq.AddTranslation("Requested parts", English, "UI", 0, Nothing, 0, False), "", parts, part, iq.Screens(719), 100, False, False, "B")

        'create some branch info - so we can render it
        Dim bi As clsBranchInfo = New clsBranchInfo(lid, "tree." & mbid, Nothing, treewidth, enumParadigm.configuringSystem, errormessages)

        branchid = mbid 'This is passed back byref and is the parent branch of all the items in the list

        'l$ = Replace(l$, vbLf, "")

        Dim abranch As clsBranch
        For Each partno In Split(l$, ";")
            If iq.i_SKU.ContainsKey(partno) Then
                mbid -= 1 'Decrement - to create descending (negative) branch ID's
                abranch = New clsBranch(mbid, iq.i_SKU(partno), HeaderBranch, iq.AddTranslation(partno, English, "UI", 0, Nothing, 0, False), "", parts, part, Nothing, 100, False, False, "G")
                '  setBranchState(lid, "tree." & branchid & "." & mbid.ToString, oc.open, bt.gridrow, False)
            Else
                errs.Add(partno & " is not in iQuote (invalid part number ?)")
            End If
        Next

        iq.sesh(lid, "swiftEnd") = mbid 'store the last (most negative) branch we created so we can clean up later

        Return errs

    End Function
    Private Function getAdverts(lid As UInt64, endpath As String) As String
        Dim adverts As Dictionary(Of Integer, clsAdvert) = New Dictionary(Of Integer, clsAdvert)
        Dim quote As clsQuote = Nothing
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim adLiteral As String = ""
        If iq.SeshContains(lid, "QuoteID") Then
            Dim quoteID As Integer = iq.seshTyped(Of Integer)(lid, "QuoteID")
            If quoteID <> 0 Then
                quote = agentAccount.Quotes(quoteID)
                If quote.RootItem.Children.Count = 0 Then quote.LoadItems(errorMessages)
            End If
        End If

        adverts.Clear()
        For Each advert As clsAdvert In From a In iq.Adverts.Values Where a.Visible = True AndAlso (a.Manufacturer = Manufacturer.Unknown OrElse a.Manufacturer = agentAccount.Manufacturer)
            If advert.Campaign.StartDate.Date <= Today.Date And advert.Campaign.EndDate.Date >= Today.Date Then
                ' If advert.Campaign.Seller Is agentAccount.SellerChannel Then
                If advert.AdRegionPresent.Encompasses(agentAccount.SellerChannel.Region) Then
                    If advert.AdRegionAbsent Is Nothing Or (advert.AdRegionAbsent IsNot Nothing AndAlso Not advert.AdRegionAbsent.Encompasses(agentAccount.SellerChannel.Region)) Then
                        If quote Is Nothing Or CType(iq.sesh(lid, "Paradigm"), enumParadigm) = enumParadigm.AddingSystem Then
                            If advert.ImageWide = False Then
                                adverts.Add(advert.ID, advert)
                            End If
                        Else
                            Dim addAdvert As Boolean = True
                            'If advert.Present IsNot Nothing Then
                            If advert.Present.Code <> "none" Then
                                If Not quote.RootItem.HasProductType(advert.Present) Then
                                    addAdvert = False
                                End If
                            End If
                            'If advert.Absent IsNot Nothing Then
                            If advert.Absent.Code <> "none" Then
                                If quote.RootItem.HasProductType(advert.Absent) Then
                                    addAdvert = False
                                End If
                            End If
                            Dim found As Boolean = False
                            If advert.SlotTypeCode IsNot Nothing Then
                                If iq.i_slotType_Code.ContainsKey(advert.SlotTypeCode) Then
                                    For Each slotType As clsSlotType In iq.i_slotType_Code(advert.SlotTypeCode).Values.ToArray()
                                        Dim systemItems As List(Of clsQuoteItem)
                                        systemItems = quote.RootItem.findSystemItems
                                        For Each itm In systemItems
                                            Dim dicslots As Dictionary(Of clsSlotType, clsSlotSummary) = New Dictionary(Of clsSlotType, clsSlotSummary)()

                                            itm.ValidateSlots2(dicslots, True) 'Recursive ! - compiles (and uses internally the quotes dicslots) =- Gives
                                            itm.ValidateSlots2(dicslots, False) 'Now for takes, to fill fallbacks
                                            itm.dicslots = dicslots

                                            Dim l = (From n In itm.dicslots.Keys Where n.MajorCode = slotType.MajorCode).ToList()
                                            If l IsNot Nothing And l.Count > 0 Then
                                                For Each s In l
                                                    Dim iloCont = itm.dicslots(s)
                                                    If iloCont.Given > 0 Then
                                                        If iloCont.taken = 0 Then
                                                            addAdvert = True
                                                        Else
                                                            addAdvert = False
                                                            found = True
                                                            Exit For
                                                        End If
                                                    End If
                                                Next
                                            Else
                                                addAdvert = False
                                            End If
                                        Next
                                        If found Then Exit For
                                    Next
                                End If
                            End If
                            If addAdvert Then
                                adverts.Add(advert.ID, advert)
                            End If
                        End If
                    End If

                End If
                ' End If
            End If
        Next


        Dim randomClass As New Random()
        Dim rndNumber As Integer
        Dim RememberSet As New HashSet(Of Integer)
        Dim adNumber As Integer = 2
        Dim imageWide As Boolean = True

        If endpath.Trim.ToLower = "tree.1" Then
            adNumber = 3
            imageWide = False
            adLiteral = "<div id=""bannerAd"" class =""bannerdivright"">"
        Else
            adLiteral = "<div id=""bannerAd"" class =""bannerdivtop"">"
        End If
        If adNumber > adverts.Values.Count Then
            adNumber = adverts.Values.Count
        End If
        '   Dim selectedAdverts = From a In adverts.Values Where a.ImageWide = imageWide
        While RememberSet.Count < adNumber AndAlso adverts.Values.Count > 0
            rndNumber = randomClass.Next(0, adverts.Values.Count)
            If RememberSet.Add(rndNumber) Then
                Dim selectedAdvert As clsAdvert = adverts.Values(rndNumber)

                If selectedAdvert IsNot Nothing Then
                    Dim advertImpressions As clsImpression = New clsImpression(agentAccount, selectedAdvert, Now)
                    Dim urlString As String = selectedAdvert.ImageUrl
                    Dim navigateURL As String = selectedAdvert.URL
                    If quote IsNot Nothing AndAlso quote.Cursor IsNot Nothing AndAlso quote.Cursor.Branch IsNot Nothing Then
                        Dim sysitem As clsQuoteItem = quote.Cursor
                        If selectedAdvert.URL.Contains("EMULEX") Then
                            Dim x = sysitem.Branch.findAllProductPathsByAttributeValueRecursive(sysitem.Path, "optType", "PCI*", True, agentAccount)
                            navigateURL = navigateURL & "|" & sysitem.Path & "|" & x.FirstOrDefault
                        ElseIf selectedAdvert.URL.Contains("ROK") Then
                            Dim x = sysitem.Branch.findAllProductPathsByAttributeValueRecursive(sysitem.Path, "optType", "SOF1", True, agentAccount)
                            navigateURL = navigateURL & "|" & sysitem.Path & "|" & x.FirstOrDefault
                        End If
                    End If
                    If String.IsNullOrEmpty(urlString) = False Then
                        adLiteral &= "<a onclick=""clickthru(" & selectedAdvert.ID & ",'" & navigateURL & "');""><img class = """ & IIf(imageWide, "bannerimgtop", "bannerimg").ToString() & """  alt="""" src=""" & urlString & """   /></a>"
                    End If

                End If
            End If

        End While
        adLiteral = adLiteral & "</div>"
        If RememberSet.Count = 0 Or (endpath.Trim.ToLower = "tree.1" And quote IsNot Nothing) Then
            adLiteral = ""
        End If

        Return adLiteral
    End Function


End Class