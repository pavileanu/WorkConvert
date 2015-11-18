Option Explicit On
Option Strict On

Imports IQ.clsBranchState


''' <summary>
''' This class gathers info about the state of the branch from the users session - Keeping things neat and tidy (saving us passing many, many paramteres to  much of the rendering code)
''' </summary>
''' <remarks></remarks>

Public Class clsBranchInfo

    Public lid As UInt64   'Log In Id (session key)
    Public path$    'read-only          'path in the tree of this branch eg. 'tree.1.4.563.8383.3993
    Public branch As clsBranch
    ' Public branchState As clsBranchState 'Holds type,  numrows (to display) etc
    Public agentAccount As clsAccount
    Public buyerAccount As clsAccount
    Public isTreeCursor As Boolean
    Private _EffectiveHeader As clsScreenHeader
    Public ReadOnly Property EffectiveHeader As clsScreenHeader
        Get
            If ScreenHeader Is Nothing Then
                Return _EffectiveHeader
            Else
                Return ScreenHeader
            End If
        End Get
    End Property
    Public ScreenHeader As clsScreenHeader

    
    Public Sub CreateMatrixHeader(descendants As Dictionary(Of clsBranch, clsVisibility), Optional United As Boolean = False)
        Dim errorMessages As List(Of String) = New List(Of String)()
        'Logic:
        Dim screenHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
        If screenHeaders Is Nothing Then
            screenHeaders = New Dictionary(Of String, clsScreenHeader) : iq.sesh(lid, "screenHeaders") = screenHeaders
        End If
        If screenHeaders.ContainsKey(Me.path) Then
            Me.ScreenHeader = screenHeaders(Me.path)
        Else
            ''        If rootPath IsNot Nothing Then
            ' If matrixHeaders.ContainsKey(Me.rootPath) Then MatrixHeader = matrixHeaders(Me.rootPath)
            ' Else
            ' If matrixHeaders.ContainsKey(Me.path) Then MatrixHeader = matrixHeaders(Me.path)
            ' End If
            '
            'Doesn't exist so we need to create one
            ' If (Me.branch.Product Is Nothing) Then ' OrElse Not Me.branch.Product.isSystem) Then
            If getBranchStateAbove(lid, path, errorMessages) IsNot Nothing Then
                If getBranchStateAbove(lid, path, errorMessages).rca = enumBt.DetailSquare Or getBranchStateAbove(lid, path, errorMessages).rca = enumBt.Branch Then
                    descendants = Me.visibleChildren(errorMessages, United, 0, 0, True, True)
                    ScreenHeader = New clsScreenHeader(Me, descendants, United)
                ElseIf descendants IsNot Nothing Then
                    ScreenHeader = New clsScreenHeader(Me, descendants, United)
                ElseIf rootPath IsNot Nothing Then ' getBranchStateAbove(lid, path, errorMessages).rca = enumBt.gridrow Then
                    _EffectiveHeader = matrixHeaderAbove(Me.lid, If(rootPath Is Nothing, path$, rootPath), errorMessages) 'provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)                  
                End If
            Else
                _EffectiveHeader = matrixHeaderAbove(Me.lid, If(rootPath Is Nothing, path$, rootPath), errorMessages) 'provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)
            End If
            'End If
        End If
    End Sub
    Public ReadOnly Property EffectiveMatrix As clsScreen
        Get
            Return MatrixAbove(Me.lid, Me.path)
        End Get
    End Property
    Public editHeader As clsEditHeader
    Public lblMatches As Label
    Public rootPath As String
    Public showAll As Boolean
    Public treeMode As Boolean
    Public branchButtons As Boolean   'Renders everything as a branch, and with extra buttons (prune, graft etc.)
    Public treeWidth As Single
    Public collectiveSingle As String  'How to refer to this branches lone child - eg 'Option','System','family'
    Public collectivePlural As String 'How to refer to this branches children (plural) - eg 'Options','Systems','families'
    Public foci As HashSet(Of String) 'Addional filtering of products by their focus attribute (Receta being the prime example)
    Public survivors As Integer  'number of rows 'suriving' the 'Quick' filtering 
    Public Paradigm As enumParadigm
    Public rownum As Integer
    Public divToFill As String 'used by processcommand to return the DivToFill (either 'path' or "tree" (for OpenTO's)
    Public userMessages As List(Of String) 'Used to conver swift and shopping list warnings/exceptions

    Public Sub setQuickFiltersVisible(visible As Boolean)

        Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
        If Not matrixHeaders.ContainsKey(Me.path) Then
            Dim bs = getbranchstate(Me.lid, Me.path)
            Dim errorMessages As List(Of String) = New List(Of String)()
            CreateMatrixHeader(visibleChildren(errorMEssages, bs.United, 0, 0, True, True))
        End If

        matrixHeaders(Me.path).QuickFiltersVisible = visible
        'EffectiveHeader.QuickFiltersVisible = visible
    End Sub

    Public Sub New(lid As UInt64, path As String)
        'super lightweight version used by keywordsearch  (outputresults)

        Me.lid = lid
        Me.path = path

        Me.showAll = CBool(iq.sesh(lid, "showAll"))
        Me.agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Me.buyerAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Me.foci = New HashSet(Of String)(Split(CType(iq.sesh(Me.lid, "foci"), String), ",").ToList)
        Me.userMessages = New List(Of String)
    End Sub

    Public Sub New(lid As UInt64, path$, lblMatches As Label, treewidth As Single, Paradigm As enumParadigm, ByRef errorMessages As List(Of String), Optional rootPath As String = Nothing)

        'Much of the BranchInfo is pulled from the session variables
        Me.lid = lid
        Me.path$ = path$
        Me.Paradigm = Paradigm
        Me.rootPath = rootPath

        Dim branchstate As clsBranchState = getbranchstate(Me.lid, Me.path)

        'If (branchstate IsNot Nothing AndAlso branchstate.rca = enumBt.Tab) Then treewidth = treewidth - CType(1, Single) Else treewidth = treewidth - CType(2.25, Single)

        'check required for editor (at the moment)
        If IsNumeric(Split(path, ".").Last) Then
            Me.branch = iq.Branches(CInt(Split(path, ".").Last))
        End If

        Me.showAll = CBool(iq.sesh(lid, "showAll"))
        Me.treeMode = CBool(iq.sesh(lid, "treeMode"))

        'If Me.ShowAll Then Stop
        Me.branchButtons = CBool(iq.sesh(lid, "branchButtons"))  '

        'build a hashset from the CD list stored in the session variable
        Me.foci = New HashSet(Of String)(Split(CType(iq.sesh(Me.lid, "foci"), String), ",").ToList)


        Me.lblMatches = lblMatches
        Me.agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Me.buyerAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        CreateMatrixHeader(Nothing)
        ''Any row rendering its chidren as gridrows will have a matrixheader
        'Me.MatrixHeader = Nothing

        'Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
        'If matrixHeaders Is Nothing Then
        '    matrixHeaders = New Dictionary(Of String, clsScreenHeader) : iq.sesh(lid, "matrixHeaders") = matrixHeaders
        'End If

        ''ML - Added rootPath so that the screen definition cannot be different for children of the currently displayed screen
        'If branchstate IsNot Nothing AndAlso branchstate.United = True AndAlso rootPath IsNot Nothing Then
        '    If Not matrixHeaders.ContainsKey(Me.path) AndAlso matrixHeaders.ContainsKey(rootPath) Then
        '        Me.matrixHeader = matrixHeaders(rootPath).Clone()
        '        matrixHeaders.Add(Me.path, Me.matrixHeader)
        '    End If
        'End If
        'If matrixHeader Is Nothing Then
        '    If matrixHeaders.ContainsKey(Me.path) Then
        '        Me.matrixHeader = matrixHeaders(Me.path)
        '    Else
        '        'Lets create a matrix here using the data from anything above

        '    End If
        'End If

        'Me.matrixHeader = matrixHeaderAbove(lid, If(rootPath Is Nothing, path$, rootPath), errorMessages) 'provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)

        ' If Me.effectiveHeader Is Nothing Then Stop

        If Me.ScreenHeader IsNot Nothing AndAlso Me.ScreenHeader.Vw IsNot Nothing Then
            Me.survivors = Me.ScreenHeader.Vw.Count
            ' If Me.Survivors = 0 Then Stop
        End If


        '      If Me.Survivors = 0 Then Stop 'remove

        '   '@@@ DELEE

        'If Me.ShowAll Then
        '    'when switching to 'showall' mode - it's possible that we're revealing siblings that have never been rendered - as such they don't have state
        '    Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        '    If Not branchStates.ContainsKey(Me.path) Then
        '        If branchStates.ContainsKey(oneAbove(Me.path)) Then
        '            branchStates.Add (me.path, New clsBranchState( ) ' create a (closed) branch like the first visible sibiling
        '        End If
        '    End If

        '  Me.branchState = getbranchstate(Me) 'returns the branchState at this path IF it Exists - It's Legitimate for htis to return NOTHING for a branch we don't want to render

        If iq.SeshContains(lid, "treeCursorPath") Then
            If LCase(path$) = CStr(iq.sesh(lid, "treeCursorPath")) Then Me.isTreeCursor = True
        End If

        Dim p() As String = Split(path, ".")

        If Not Me.agentAccount Is Nothing Then
            If p.Count > 0 Then
                Dim parentbranch As clsBranch = iq.Branches(CInt(p.Last))
                Me.collectiveSingle = parentbranch.collectiveNounSingular.text(Me.agentAccount.Language) 'get the name of the things we're viewing from the first one of them (e.g. servers)
                Me.collectivePlural = parentbranch.CollectiveNoun.text(Me.agentAccount.Language)
            Else
                Me.collectiveSingle = iq.RootBranch.collectiveNounSingular.text(Me.agentAccount.Language) 'get the name of the things we're viewing from the first one of them (e.g. servers)
                Me.collectivePlural = iq.RootBranch.CollectiveNoun.text(Me.agentAccount.Language)
            End If
        End If

        Me.branchButtons = branchButtons
        Me.treeWidth = treewidth
        Me.userMessages = New List(Of String)


    End Sub

    Public Function MoreThanXskus(max As Integer) As Boolean

        Dim errorMessages As New List(Of String)
        Dim found As Integer = 0
        Me.branch.getSKUdDescendants(False, Me.path, Me, False, max, found, errorMessages)

        If found >= max Then Return True

    End Function

    ''' <summary>Fetch either the direct descendants (which may be categories) or the SKUD descendants (products) </summary>
    ''' <param name="errorMessages"></param>
    ''' <param name="united"></param>
    ''' <param name="skus"></param>
    ''' <param name="cats"></param>
    ''' <param name="fbv">whether to filter and sort (the descenants) by the view (i.e. filters) in force</param>
    ''' <param name="filter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function visibleChildren(ByRef errorMessages As List(Of String), united As Boolean, ByRef skus As Integer, ByRef cats As Integer, fbv As Boolean, filter As Boolean, Optional checkSystem As Boolean = False) As Dictionary(Of clsBranch, clsVisibility)

        Dim vc As Dictionary(Of clsBranch, clsVisibility)

        If united Then
            If checkSystem Then
                vc = Me.branch.getSKUdDescendants(False, Me.path, Me, Me.branch.Product.isSystem(), 10000, 0, errorMessages)
            Else
                vc = Me.branch.getSKUdDescendants(False, Me.path, Me, False, 10000, 0, errorMessages)
            End If
            'these might be options - or systems
            cats = 0 : skus = vc.Count
        Else
            vc = Me.branch.getVisibleChildren(Me, errorMessages, skus, cats)
            '    If vc.Count = 0 Then Stop

        End If

        If CType(iq.sesh(lid, "Paradigm"), enumParadigm) = enumParadigm.configuringSystem AndAlso Not isSystemAbove(lid, path) Then
            Dim vcremove As List(Of clsBranch) = New List(Of clsBranch)()
            For Each v In vc
                If CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState)).ContainsKey(v.Value.path) AndAlso CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))(v.Value.path) IsNot Nothing Then
                Else
                    vcremove.Add(v.Key)
                End If
            Next
            For Each k In vcremove
                vc.Remove(k)
            Next
        End If


        'the root view may not be populated
        If Me.ScreenHeader IsNot Nothing Then
            If Me.ScreenHeader.Vw Is Nothing Then
                Return vc
            End If
        End If

        If filter = False Then Return vc 'important

        Dim eh As clsScreenHeader = Nothing 'When recalcualting filters - we need to NOT apply the view at this level - but DO apply upper filters in effect
        Dim ehp As String = Me.path 'effective header path
        If fbv = False Then ehp = Left(ehp, InStrRev(ehp, ".") - 1) 'filter by view
        If ehp = "tree" Then Return vc

        eh = EffectiveHeader 'matrixHeaderAbove(Me.lid, ehp, errorMessages)

        If fbv AndAlso eh IsNot Nothing AndAlso eh.Vw IsNot Nothing AndAlso eh.Vw.Count > 0 Then
            eh.RefreshViewFilter()
            Dim filteredAndSorted As New Dictionary(Of clsBranch, clsVisibility)
            With eh

                'If the visibleChildren have SKUs they're products, otherwise they're categories
                '**There can never be a mix ! **
                If vc.Values.Count > 0 AndAlso vc.Values.First.branch.HasSKU Then
                    'products
                    'we iterate over the view - becuase it provides the sort
                    Dim cc As Integer = .Vw.Count
                    For i As Integer = 0 To .Vw.Count - 1
                        Dim branch As clsBranch = iq.Branches(CInt(.Vw(i).Item("id")))
                        Dim pn$ = branch.Product.DisplayName(English)

                        'There ARE some things in the view - which have now been determined as not visible
                        If vc.ContainsKey(branch) Then
                            filteredAndSorted.Add(branch, vc(branch))
                        Else
                            '                            Beep()
                        End If
                    Next
                Else
                    'categories - these aren't in the view.. but are in VC
                    'Temporarily change sort of table for the .find function which MUST be by ID
                    Dim s = .Vw.Sort
                    .Vw.Sort = "[ID]"
                    For Each vis In vc.Values.OrderBy(Function(c) c.branch.order) 'vc are the visible children (clsVisibility)  - they may be SKU'd product branches or SKUless category branches
                        If vis.branch.isInOrHasDescendantIn(.Vw) OrElse showAll Then
                            filteredAndSorted.Add(vis.branch, vis)
                        End If
                    Next
                    .Vw.Sort = s
                End If
            End With

            Return filteredAndSorted

        Else
            Return vc
        End If


    End Function

    Public Sub switchTo(bt As enumBt, ByRef errorMessages As List(Of String))

        Dim branchstate As clsBranchState = getbranchstate(Me.lid, Me.path)

        ' Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        'branchState = branchStates(Me.path)

        If branchstate Is Nothing Then
            errorMessages.Add("branchstate was nothing for path me.path in switchto ")
        Else
            branchstate.rca = bt

            'We already have state for this branch so it must be a view switch
            'IF there is an explict type specified as a paramater - (ie, we used the switcher)
            If bt = enumBt.errorNotSet Then
                Beep()
            End If

            Select Case bt
                Case Is = enumBt.DetailSquare
                    If Not branchstate.United = False Then InvalidateMatrixBelow(Me.path)

                    Dim descendants As Dictionary(Of clsBranch, clsVisibility) = Me.visibleChildren(errorMessages, True, 0, 0, True, True)
                    branchstate.rca = enumBt.DetailSquare ' Ok - now Render your Children As detailed squares 

                    Me.CreateMatrixHeader(descendants, False)
                    branchstate.United = False
                Case Is = enumBt.gridrow
                    If Not branchstate.United = True Then InvalidateMatrixBelow(Me.path)
                    branchstate.United = True

                    CloseBelow(Me.lid, Me.path) 'this is really important - we need to kill all state below the branch we're swithing to a grid (otherwise (for example) supply chains will cause gridrows to render as branches - see GetBranchstateAbove)

                    'potential very large speedup here - by checking whether the matrixHeader and its associated datatable/view already exits
                    '(in the sesh)
                    'we would not then need to fetch the descendants or rebuild the matrixheader

                    'Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
                    'If (matrixHeaders.ContainsKey(Me.path)) Then
                    '    Me.MatrixHeader = matrixHeaders(Me.path)
                    'Else

                    Dim descendants As Dictionary(Of clsBranch, clsVisibility) = Me.visibleChildren(errorMessages, True, 0, 0, True, True)

                    branchstate.rca = enumBt.gridrow ' Ok - now Render your Children As gridrows 

                    Me.CreateMatrixHeader(descendants, False)

                    'Me.switchToGrid(descendants, errorMessages) 'This is really important
                    '       Me.MatrixHeader.rebuild(Me, descendants, errorMessages) 'became..
                    'End If

                Case Else
                    If Not branchstate.United = False Then InvalidateMatrixBelow(Me.path)
                    branchstate.United = False

            End Select
        End If

    End Sub

    Sub InvalidateMatrixBelow(path As String, Optional IncludeActualPath As Boolean = False)
        Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
        For Each s As String In matrixHeaders.Keys.ToList().Where(Function(a) a.StartsWith(path) And (IncludeActualPath Or a <> path))
            matrixHeaders.Remove(s)
        Next
    End Sub

    'Public Function QuoteContains(product As clsProduct) As Boolean


    '    If iq.SeshContains(Me.lid, "QuoteID") Then
    '        Dim quoteID As Integer
    '        quoteID = CInt(iq.sesh(lid, "QuoteID"))
    '        If quoteID <> 0 Then
    '            Dim quote As clsQuote = CType(iq.sesh(Me.lid, "AgentAccount"), clsAccount).Quotes(quoteID)

    '            Dim j = F-rom i In quote.RootItem.Flattened(False, False, 0).items Where i.QuoteItem.Branch.Product Is product
    '            If j.Any Then QuoteContains = True

    '        End If
    '    End If

    'End Function



    Public Sub close(ByRef errorMessages As List(Of String))

        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        branchStates.Remove(Me.path)
        'branchStates(Me.path).rca = enumBt.Hidden  'we KEEP the state, and set its RCA as hidden (this is a bit like a 'visited' hyperlink) - if we were to delete the state, we have no way to store closed branches (specifically to mix opened and closed branches at any given level)

    End Sub


    Public Function open(ByRef errorMessages As List(Of String), openFirstTab As Boolean) As clsBranchState 'BranchState is a RERERENCE to an object utimately stored under sesh(lid)

        'open this branch - and set how it renders its children based upon the type depth, existing state of the parent (being opened)

        Dim branchstate As clsBranchState = getbranchstate(Me.lid, Me.path)

        'Just In Time Carepacks 
        If Me.branch.HasSKU AndAlso Me.branch.Product.isSystem Then
            ' Me.Branch.createCarePacks(Me.path, Me.BuyerAccount)
        End If

        'If Me.path = "tree.1" Then Stop


        If branchstate Is Nothing OrElse branchstate.rca = enumBt.Hidden Then
            If branch.rca = "" Then
                errorMessages.Add("RCA for Branch " & Me.branch.ID.ToString & " was not set defaulting to B")
                branch.rca = "B"
            End If

            Dim unite As Boolean = CBool(branch.rca.First = "G")
            Dim bt As enumBt
            bt = CType(BTchar.IndexOf(Me.branch.rca.First), enumBt) 'use the default form from the branches RCA
            If branchstate Is Nothing Then
                branchstate = New clsBranchState(Me.lid, Me.path, bt, unite, Me.rownum, 100)
            Else
                branchstate.rca = bt
                branchstate.United = unite
            End If
        End If

        'this is the Adding Systems 'Paradigm' - we hide the options (f*cking insanity - but ours is not to reason why)
        'put that another way - we only show the option tabs on a system when we have one of them in the basket
        '(WHY they couldn't just be collapsed - i don't know)
        '    If Me.Paradigm = enumParadigm.AddingSystem And Me.Branch.Product IsNot Nothing AndAlso Me.Branch.Product.isSystem Then
        'If Not Me.QuoteContains(Me.Branch.Product) Then
        ' branchstate.rca = enumBt.Hidden
        ' End If

        If branchstate.rca = enumBt.gridrow Or branchstate.rca = enumBt.Tab Then
            'If Branch.childBranches.Count = 0 Then Stop
            Dim fbv As Boolean
            If treeMode Then fbv = False
            Dim descendants As Dictionary(Of clsBranch, clsVisibility) = Me.visibleChildren(errorMessages, branchstate.United, 0, 0, fbv, True)

            'If descendants.Count = 0 Then Stop

            'It is Legitimate for a Branch with no children to be open(ed) .. They render differenty (eg. options may dispay their attributes when open)
            '  If descendants.Count = 0 Then Stop

            If branchstate.rca = enumBt.gridrow Then

                'If we're opening up a grid... we need to create a matrixheader
                If descendants.Count > 0 And Me.ScreenHeader Is Nothing Then  'NOT at the level of an option (with no descendants) though - or bad things happen
                    Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
                    If Not matrixHeaders.ContainsKey(Me.path) Then
                        CreateMatrixHeader(descendants, False)

                        'Me.MatrixHeader = mh 'Added 150914 ML, header was not populating on refresh as it was never assigned to this branch info although it was added to the sesh
                    End If
                End If

            ElseIf branchstate.rca = enumBt.Tab And openFirstTab Then
                'I'm rendering my children as tabs - autoOpen the first one
                'autoOpenFirstTab()

                Dim tabOpen As Boolean = False
                Dim firstTab As clsBranchInfo = Nothing
                For Each vi In descendants.Values 'From j In descendants.Values Order By iq.Branches(CInt(Split(j.path, ".").Last)).order
                    Dim tbs As clsBranchState = getbranchstate(Me.lid, vi.path)
                    If firstTab Is Nothing Then
                        firstTab = New clsBranchInfo(Me.lid, vi.path, Nothing, Me.treeWidth, Me.Paradigm, errorMessages)
                    End If

                    If tbs IsNot Nothing AndAlso tbs.rca <> enumBt.Hidden Then
                        tabOpen = True
                        Exit For
                    End If
                Next
                If Not tabOpen Then
                    If firstTab IsNot Nothing Then  'clicking the hyperlink twice do descendants here
                        firstTab.open(errorMessages, openFirstTab) 'if none of the tabs in this set are open - we open the first (actually this just sets state - it will be rendered open)
                    End If
                End If
            End If
        End If


        '@@ moved from Processcommand - becuase branches are also .opened from ploughpath
        If branchstate IsNot Nothing Then
            If branchstate.rca = enumBt.OpenBranch Then 'AUTOOPEN - 'O' type branches
                For Each CB In Me.branch.childBranches.Values
                    Dim NBI As clsBranchInfo = New clsBranchInfo(Me.lid, Me.path & "." & CB.ID, Nothing, Me.treeWidth, Me.Paradigm, errorMessages)
                    NBI.open(errorMessages, False)
                Next
            End If
        End If


        Return branchstate

    End Function

    'Public Sub prepareGridView(ByRef errormessages As List(Of String))

    '    'work out the availablewidth emsAvailable
    '    Dim pth As String = ""
    '    Dim w As Single = Me.treeWidth  'The full width of the left pane

    '    w -= 1  'A little bit of breathing space

    '    For Each seg In Me.path.Split(".".ToArray)
    '        pth &= seg

    '        If pth <> "tree" Then
    '            Dim bs As clsBranchState = getbranchstate(Me.lid, pth)
    '            If bs IsNot Nothing Then  'we may cross some branches that have never been rendered (they are united) - hence have no state
    '                Select Case bs.rca
    '                    Case Is = enumBt.OpenSquare 'BreadCrumb
    '                        w = Me.treeWidth
    '                    Case Is = enumBt.Branch
    '                        w = CSng(w - 2.25) 'ems indent 'was 2.5
    '                    Case Is = enumBt.Tab, enumBt.gridrow
    '                        '   w = CSng(w - 1) 'ems indent 'was 2.5
    '                    Case Else
    '                        '   w = CSng(w - 2.25) '- this causes otpions squishing
    '                        '  Beep()

    '                End Select

    '                If Paradigm = enumParadigm.configuringSystem And iq.Branches(CInt(seg)).hassystem Then w = treeWidth

    '            End If
    '        End If

    '        pth &= "."
    '    Next

    '    Me.MatrixHeader.CollapseColumns(w, errormessages)

    '    ''we MUST kill deeper matrixheaders (otherwise individual 'united' rows can render with the wrong header set - this will need to change
    '    'Dim tokill As List(Of String) = New List(Of String)
    '    'For Each p In matrixHeaders.Keys.ToArray
    '    '    If plen(p) > plen(Me.path) Then
    '    '        If Left$(p, Len(Me.path$)) = Me.path Then
    '    '            matrixHeaders.Remove(p)
    '    '        End If
    '    '    End If
    '    'Next p

    'End Sub

    Function PathLevel() As Integer
        Return path.Split(CChar(".")).Length - 1
    End Function


End Class


'        Return branchstate

'Else
'Beep()

'    If CType(iq.sesh(lid, "treeMode"), Boolean) = True Then
'        bs = New clsBranchState(Me.lid, Me.path, bt.Branch, False, 0, Me.rownum, 1000)
'        Return Me
'    End If

'    If Me.Branch.Picture = "hptop" Then
'        bs = New clsBranchState(Me.lid, Me.path, bt.TROhead, False, 0, Me.rownum, 1000)
'        Return Me
'    End If

'    'If .rca = bt.TROhead Then
'    ' .rca = bt.TROitem
'    ' Return Me
'    ' End If

'    '   If .rca = bt.BreadCrumb Then  'clicked on a breadcrumb
'    '.state = oc.open
'    'setChildBranches(oc.closed, bt.Square, OpenWhich.None, errorMessages) 'and render my children squares
'    ' Else
'    'Dim openWhich As OpenWhich = openWhich.None
'    'Dim openAs As bt = .renderAs 'What we will open the children as .. Inherit this branches type by default  . . 

'    Dim parentPath As String = ""
'    Dim wasSquare As Boolean = False
'    'Special handling for opening a branch that is currently a square..


'.United = False
'        'wasSquare = True
'    ElseIf pbs.rca = bt.Tab Then

'        'autoOpen the first tab only if there is no state 
'        If getbranchstate(lid, Me.path) Is Nothing Then
'            openWhich = openWhich.First
'        End If
'    End If

'    .state = oc.open  'open me

'    'Tim mode (opening a family) - opens the supply chains - shows all models as branches
'    If seg.Length = 4 Then
'        bs = New clsBranchState(Me.lid, Me.path, bt.Branch, False, 0, Me.rownum, 1000)

'        'If .renderAs = bt.errorNotSet Then
'        openAs = bt.Branch
'        ' End If
'        .United = False
'        .renderAs = bt.Branch

'        'Nick Mode
'        ' openWhich = openWhich.None : .United = True : openAs = bt.gridrow
'    End If

'    If Me.Branch.Product IsNot Nothing Then
'        If Me.Branch.Product.isSystem Then
'            ' .renderAs = bt.Branch

'            If Paradigm = enumParadigm.AddingSystem Then
'                .openWhich = openWhich.None 'for greg (STUPID idea but who am I to argue)
'                .rca = bt.hidden
'            ElseIf Paradigm = enumParadigm.configuringSystem Then
'                .rca = bt.Tab
'                'If stateBelow(Me.lid, path) Then
'                '    openWhich = openWhich.None 'if a tab is already open - don't reopen the first
'                'Else
'                '    openWhich = openWhich.First

'                'End If
'                '                            .rcenderAs = bt.headless
'            Else
'                Beep()
'            End If

'            .United = False
'        End If
'    End If

'    'new for dan
'    If seg.Length = 8 Then
'        .rca = bt.Branch
'        .United = False
'        .openWhich = openWhich.None
'    End If

'    Dim descendants As Dictionary(Of clsBranch, clsVisibility) ' = Me.visiblechildren(setChildBranches(oc.closed, openAs, openWhich, errorMessages)

'    If seg.Length = 9 Then
'        .rca = bt.gridrow  'Show grids inside the nested option tabs
'        .United = True
'        .openWhich = openWhich.None
'        descendants = Me.visiblechildren(errorMessages) 'setChildBranches(oc.closed, openAs, openWhich, errorMessages)
'        Me.switchToGrid(descendants, errorMessages) 'Creates a set of grid headers (holding filters, sorts, column widths etc.) for this user session
'    End If


'    'generally we come through here ..
'    ' Me.setChildBranches(oc.closed, openAs, openWhich, errorMessages)

'    'for squares - we must force it to re-render from the parent
'    If wasSquare Then Return New clsBranchInfo(Me.lid, parentPath, Nothing, Me.treeWidth, Me.Paradigm, errorMessages)

'End If
'End With
'End If

'Return Me 'this is VITAL (to return some branchinfo) 
