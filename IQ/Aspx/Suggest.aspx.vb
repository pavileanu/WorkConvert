'Option Strict On
Imports System.Linq
Imports System.Reflection

Public Class suggest
    Inherits clsPageLogging

    Public searchID As Integer
    Public abandon As Boolean = False

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'If iq.sesh(lid,"AgentAccount") Is Nothing Then
        ' Response.StatusCode = 401 : Response.End() 'return a '401' which will be detected by the ajax and cause a redirect to signin.aspx
        ' Else

        'NB: This is used by the keyword search and from the editor
        Dim lid As UInt64

        Try
            lid = Request.QueryString("lid")
        Catch ex As System.Web.HttpRequestValidationException 'Validate that the request query string is not deemed as dangerous (Javascript injection etc), if so return nothing back
            Return
        End Try

        'if a user triggers many searches (by for example.. typing!) - we should abandon any in progress


        iq.nextSearchID += 1
        searchID = iq.nextSearchID

        'if the session variable searchID ever diverges from the local variable searchID -
        ' this (same) user has started another search - and this search can be abandoned.
        iq.sesh(lid, "searchID") = searchID



        Dim buyerAccount As clsAccount
        Dim agentAccount As clsAccount
        buyerAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Dim language As clsLanguage
        If Request.QueryString("lid") Is Nothing Then
            language = English
        Else
            lid = Request.QueryString("lid")
            language = agentAccount.Language
        End If

        Dim ru$
        ru$ = Request.RawUrl

        Dim fl As Integer
        Dim frag As String
        Dim lit As New Literal
        frag = LCase(Request("frag")) 'the frag is the contents of the seach textbox - the context of a keyword search it may be several words (or comma seperated phrases)
        fl = frag.Length
        lit = New Literal
        lit.Text = "!Begin"

        Form.Controls.Add(lit)

        'You can pas a "path" e.g. Path=channel(1).CustomerAccounts
        'OR 
        'a root level dictioanry and (optional) filter e.g. dic=states(group=TH)

        Dim path$ = Request("path")  'This is an object model path - for the editor - NOT a searchPath

        Dim searchpath = Request("searchPath") 'keyword search path

        Dim qs As String = Request.RawUrl

        Dim filter$

        Dim obj As Object = iq, dic As Object = Nothing
        If path$ <> "" Then
            filter = ParsePath(path$, obj, dic, errorMessages)
            'DicSearch(LCase(frag), dic, language, errorMessages, filter)

        Else

            If Request("dic") = "keywords" Then
                If frag <> "" Then
                    Form.Controls.Add(KeywordSearch(agentAccount, buyerAccount, LCase(frag), Request("searchType"), searchpath$, errorMessages))
                End If

            ElseIf Request("dic") = "accounts" Then  'the account search (from the opening quote screen - has very different presentation and additional functionality (for creating new Channels and users)
                'AccountSearch(LCase(frag), agentAccount.SellerChannel.CustomerAccounts, agentAccount.Language)
                AccountSearch(LCase(frag), iq.Accounts, agentAccount.Language)  'we need to search the GLOBAL list of accoutns
            ElseIf Request("dic") = "translation" Then
                TranslationSearch(LCase(frag), agentAccount.Language)

            Else

                'var url = "suggest.aspx?valueBox=&" + valueboxID + "&textBoxID=" + textBoxID + "&frag=" + textBox.value + "&divID=" + divID + "&dic=" + dicName 

                'Search a root dictionary - with an optional filter
                'find the root level dictionary this field looks up values in

                'this should be consolidated/replaced with the above

                Dim lu$ = Request("dic")
                filter$ = GetParenthesisValue(Request("dic"))
                Dim op As Integer = InStr(lu$, "(")
                If op > 0 Then
                    dic = Reflection.WalkPropertyValue(iq, Left$(lu$, op - 1), errorMessages) 'could go pack to getpropertyvalue for a slight speedup
                Else
                    dic = Reflection.WalkPropertyValue(iq, lu$, errorMessages)
                End If

                Dim myLit As Literal = Nothing
                Dim div$
                Dim valuebox As String = Request("ValueBoxID")
                Dim textBoxID As String = Request("textBoxID")
                Dim divid As String = Request("divid")
                div$ = "<div class=|dropDownRow| onclick=|document.getElementById('" & valuebox & "').value='null';document.getElementById('" & textBoxID & "').value='None';display('" & divid & "','none');|>"
                div$ &= "None('null')"
                div$ &= "</div>"
                myLit = New Literal
                myLit.Text = Replace(div$, "|", Chr(34))
                Form.Controls.Add(myLit)

                For Each kvp In SearchResults(LCase(frag), dic, language, errorMessages, filter$, 1000)
                    '                                                                     \/ this is an ID (of an object)
                    div$ = "<div class=|dropDownRow| onclick=|document.getElementById('" & valuebox & "').value=" & kvp.Key & ";document.getElementById('" & textBoxID & "').value='" & kvp.Value & "';display('" & divid & "','none');|>"
                    div$ &= kvp.Value & "(" & kvp.Key & ")"
                    div$ &= "</div>"

                    myLit = New Literal
                    myLit.Text = Replace(div$, "|", Chr(34))
                    Form.Controls.Add(myLit)

                Next
            End If
        End If

        If errorMessages.Count > 0 Then
            OutputErrors(Form.Controls, errorMessages, lid)
        End If


        lit = New Literal
        lit.Text = "!End"
        Form.Controls.Add(lit)

    End Sub

    Public Sub AccountSearch(frag$, dicAccounts As Dictionary(Of Integer, clsAccount), Language As clsLanguage)

        Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)

        Dim sellerChannel As clsChannel = CType(iq.sesh(lid, "AgentAccount"), clsAccount).SellerChannel

        'we uses leading spaces plus the fragment to find words beggining with the frag
        Dim m =
            From a In dicAccounts.Values
            Where (LCase(" " & a.BuyerChannel.DisplayName(Language)).Contains(" " & frag) Or LCase(" " & a.Priceband.text).Contains(" " & frag) Or LCase(" " & a.User.RealName).Contains(" " & frag))
            Order By a.SellerChannel.ID = sellerChannel.ID Descending, a.BuyerChannel.ID, a.User.ID

        'Dim m = From a In dicAccounts.Values Where LCase(a.User.RealName.Contains(frag)) Order By a.displayName(Language) ' Take 15  '  Downt work as expected -> Or a.User.RealName Like "*" & frag & "*"

        Dim count As Integer = 0
        Dim OA As clsAccount = Nothing

        'we will iterate all the matching accounts outputting the first account for each buyer company - which will be wi
        'note:- accounts link a buyer channel to a sellerchannel and can be considred as belonging to a (buying) user
        'a single buyng company may therefore have many accounts with a selling channel (one for each buyer) - they will all have the same priceBand
        Dim nextUser As Boolean = False
        Dim nextCompany As Boolean = False

        Dim lit As Literal

        For Each Ac As clsAccount In m  'any account that is with the current seller will apear first

            nextUser = False
            nextCompany = False
            If OA Is Nothing Then
                nextUser = True
                nextCompany = True
            Else
                If Ac.User IsNot OA.User Then nextUser = True
                If Ac.BuyerChannel IsNot OA.BuyerChannel Then nextcompany = True
            End If

            If nextUser Then

                'If nextCompany Then
                '    If Not OA Is Nothing Then
                '        lit = New Literal
                '        lit.Text = "-" & OA.ID & "^--------New contact" & "]"  'We return NEGATIVE the ID of the last account in the PREVIOUS company - to create a sibiling account of
                '        Form.Controls.Add(lit)
                '    End If
                'End If

                If nextCompany Then
                    lit = New Literal
                    'We return NEGATIVE the ID of the first account in the company - to create a sibiling account of it
                    lit.Text = -Ac.ID & "^" & Ac.BuyerChannel.DisplayName(Language) & " - " & Ac.BuyerChannel.Address & "- New contact]"
                    Form.Controls.Add(lit)
                End If

                lit = New Literal
                lit.Text = Ac.ID & "^--------" & Ac.User.RealName & " - " & Ac.Priceband.text & "]"
                Form.Controls.Add(lit)

            End If

            OA = Ac

            count += 1
            If count = 50 Then
                lit = New Literal
                lit.Text = Xlt("0^This list is incomplete - please type more of the contact or company name to narrow the results]", Language)  'Return -1 for the 'new company' option
                Form.Controls.Add(lit)

                Exit For
            End If

        Next

        lit = New Literal
        lit.Text = Xlt("-1^New Company]", Language)  'Return -1 for the 'new company' option
        Form.Controls.Add(lit)


    End Sub
    Public Sub TranslationSearch(frag$, Language As clsLanguage)

        Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)

        Dim sellerChannel As clsChannel = CType(iq.sesh(lid, "AgentAccount"), clsAccount).SellerChannel


        '  Dim translationResults As clsTranslation
        'we uses leading spaces plus the fragment to find words beggining with the frag
        Dim m =
            From a In iq.Translations.Values
            Where (LCase(" " & a.textTranslation(Language)).Contains(frag))
            Order By a.Order

        'Dim m = From a In dicAccounts.Values Where LCase(a.User.RealName.Contains(frag)) Order By a.displayName(Language) ' Take 15  '  Downt work as expected -> Or a.User.RealName Like "*" & frag & "*"

        Dim count As Integer = 0
        Dim OA As clsAccount = Nothing

        'we will iterate all the matching accounts outputting the first account for each buyer company - which will be wi
        'note:- accounts link a buyer channel to a sellerchannel and can be considred as belonging to a (buying) user
        'a single buyng company may therefore have many accounts with a selling channel (one for each buyer) - they will all have the same priceBand
        Dim nextUser As Boolean = False
        Dim nextCompany As Boolean = False

        Dim lit As Literal


        Dim myLit As Literal = Nothing
        Dim div$
        Dim valuebox As String = Request("ValueBoxID")
        Dim textBoxID As String = Request("textBoxID")
        Dim divid As String = Request("divid")

        For Each tr As clsTranslation In m
            div$ = "<div class=|dropDownRow| onclick=|document.getElementById('" & valuebox & "').value=" & tr.Key & ";document.getElementById('" & textBoxID & "').value='" & tr.textTranslation(Language) & "';display('" & divid & "','none');|>"
            div$ &= tr.textTranslation(Language) & "(" & tr.Key & ")"
            div$ &= "</div>"
            lit = New Literal
            lit.Text = Replace(div$, "|", Chr(34))
            Form.Controls.Add(lit)

            count += 1
            If count = 50 Then
                lit = New Literal
                lit.Text = Xlt("0^This list is incomplete - please type more of the contact or company name to narrow the results]", Language)  'Return -1 for the 'new company' option
                Form.Controls.Add(lit)

                Exit For
            End If

        Next

        'lit = New Literal
        ''lit.Text = Xlt("-1^New Company]", Language)  'Return -1 for the 'new company' option
        'Form.Controls.Add(lit)


    End Sub
    Public Function SearchResults(frag$, dic As Object, language As clsLanguage, ByRef errorMessages As List(Of String), Optional filter$ = "", Optional maxResults As Int32 = 10) As Dictionary(Of Integer, String)

        'A case insensitive leftmost search on the specified dictionaries values 'displayname'
        'with an optional simple filter on some Property=Value  e.g. code=threads
        'used by AutoSuggest (in the editor amongtsh other places)

        'returns a list of ^ delimited ID^Text^Color strings

        SearchResults = New Dictionary(Of Integer, String)

        Dim matches As Integer
        Dim vn As String
        Dim Lit As Literal = Nothing

        Dim rdic As Dictionary(Of String, Object)  'results dictionary (of matching entities)
        rdic = New Dictionary(Of String, Object)

        Dim fl As Integer = Len(frag$)
        Dim txt As String

        Dim prop As String = ""   'This property of the object we're about to add to the DDL . . .
        Dim mask As String = ""  'must match this (literal string)
        If filter <> "" Then
            Dim pv() As String = Split(filter, "=")
            prop = pv(0)
            mask = pv(1)
        End If

        Dim showall As Boolean
        If Request("showAll") IsNot Nothing Then
            If Request("showAll") = True Then showall = True 'used when the box is initially displayed (with a selected value)
        End If

        Dim filterOK As Boolean

        For Each v In dic.values  'could easily be converted to use LINQ for a possible speedup
            'If dic Is iq.States Then colour = v.colour Else colour = "#ffffffff"

            txt = v.displayname(language)
            vn = LCase(Left$(txt, fl))
            If vn = frag Or showall = True Then
                'check them against the filter here - (using reflection)
                If filter$ = "" Then
                    filterOK = True
                Else
                    filterOK = (Reflection.WalkPropertyValue(v, prop, errorMessages) = mask)
                End If
                If filterOK Then

                    SearchResults.Add(v.ID, txt)

                    matches += 1
                    If matches > maxResults Then Exit For
                End If
            End If
        Next

        'Lit = New Literal
        'Lit.Text = "!End"
        'Form.Controls.Add(Lit)

    End Function

    Public Function KeywordSearch(agentAccount As clsAccount, buyerAccount As clsAccount, searchText As String, searchType As String, path$, ByRef errorMessages As List(Of String)) As Panel

        'Searchtype is 'add','priced', or 'Stocked' - as determined by the radio buttons in the front end
        'searchScope is 'global' or 'local' - ie. from the RootBranch - Or from the TreeCursor

        Dim lid As UInt64 = Request.QueryString("lid")
        'TODO - by keyword searching every graft first - we could know which sets of branches have which scores

        'find our start point in the tree (the branch represented by the current treecursor)
        'This code was desigent to work from any 'entry point' in the tree - in practise it's used either from the root branch for a 'global ' search, or from a system - for an options search

        Dim startBranch As clsBranch

        Dim crossSystems As Boolean = False 'whether to 'cross systems' when recursing (i.e. is ths an options search ?)
        Dim isDiagView As Boolean = AccountHasRight(lid, "DIAGVIEW")
        If path$ = "" Then
            startBranch = iq.RootBranch
            path = iq.sesh(lid, "Root")
            crossSystems = False
            If isDiagView Then crossSystems = True 'allow a deep search through systems for diagview
        Else
            Dim seg = Split(path, ".")
            startBranch = iq.Branches(CInt(seg(UBound(seg))))
            crossSystems = True
        End If

        If Len(searchText) > 2 And searchText <> "enter a sku to find" Then

            Dim t As Double
            Dim et As Integer
            t = Stopwatch.GetTimestamp

            'Fetch a score for every branch - a total number of matches and bitwise flags for the fragments matched.

            'Dim p As clsProduct

            '10% of the time
            Dim branchScores As Dictionary(Of Integer, clsKwScore) = ScoreBranches(Fragments(searchText), agentAccount.Language)

            Dim PathScore As Dictionary(Of String, Integer)
            PathScore = New Dictionary(Of String, Integer) 'paths>scores

            'recurse every path 

            PathScore.Clear()
            Dim numsegs As Integer
            Dim segs As Array = segments(path, numsegs) 'records the segments of the path to each matching point
            'Important we start at a depth of numsegs (prepared segments - from the treecursor path) - so as to construct complete and valid paths


            If Not abandon Then
                Dim pth$ = ""
                'cacehing the score/matched bits of a branch (and it's descendats) would improve the speed of options search (but do nothign for a systems search)
                startBranch.Score(branchScores, segs, 0, 0, 0, 0, 0, numsegs - 1, PathScore, buyerAccount, searchType, crossSystems, lid, searchID, abandon, 0, pth)  '<< THIS is where all the hard work happens (Deeply recursive)

            End If

            et = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000
            If abandon Then
                Dim apanel As Panel = New Panel


                apanel.Controls.Add(NewLit("<p>Abandoned search at " & et & " milliseconds</p>"))
                Return apanel
            Else
                Return outputResults(lid, PathScore, searchType, buyerAccount, agentAccount.Language, errorMessages, crossSystems, isDiagView, et)
            End If


        Else
            Return New Panel
        End If

    End Function

    Private Function outputResults(lid As UInt64, results As Dictionary(Of String, Integer), searchtype As String, buyeraccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String), isoptionsSearch As Boolean, isDiagView As Boolean, et As Integer) As Panel

        outputResults = New Panel
        outputResults.ID = "KWresultSet"
        outputResults.CssClass = "KWresultSet"

        Dim NeedUpdate As List(Of clsVariant) = Nothing

        If CBool(buyeraccount.SellerChannel.priceConfig And 8) Then  'customer specific (webservice) pricing
            NeedUpdate = New List(Of clsVariant) 'Dictionary(Of String, ClsProductVariant)
        End If

        outputResults.Controls.Add(NewLit("<p>" & et & " milliseconds</p>"))


        'We have to go through all the results becuase we don't know what visible yet - the search is conducted on *all* branches/products
        '(except we dont recurse into systems not in the feed)

        ' Dim topscores As Dictionary(Of String, Integer)
        Dim topscores = From r In results Where r.Value > 0 Order By r.Value Descending

        Dim maxValue As Integer = topscores(0).Value

        Dim maxOccurs As Integer = Aggregate v In topscores Where v.Value = maxValue Into Count() ' Link query to find how many time the max score occurs if it is one then its a sku
        Dim lit As Literal

        'the keys are the paths, the values are the scores
        Dim branch As clsBranch
        Dim resultRow As Panel

        Dim output As Integer = 0
        Dim cc = topscores.Count

        Dim maxresults As Integer = 25
        If isDiagView Then maxresults = 500


        Dim rfh As String
        For Each kvp In topscores
            'populate branch by surfing down the path held in the key
            branch = iq.Branches(Split(kvp.Key, ".").Last)

            rfh = ""  'Readons For Hide !

            Dim include As Boolean = False 'Default to false (don't display)
            Dim greyed As Boolean = False


            'GetSkudDescendants - with 'includeself' will gives us a set of hidereaons for a given branch/product
            Dim BI As clsBranchInfo = New clsBranchInfo(lid, "tree" & kvp.Key)
            Dim sd As Dictionary(Of clsBranch, clsVisibility) ' = BI.visibleChildren(errorMessages, True, 0, 0, False, False)
            sd = New Dictionary(Of clsBranch, clsVisibility)
            'AM I Visible - first param is IncludeSelf
            BI.showAll = True 'We WANT all branches - with any of their resonsfor hide returned
            sd = branch.getSKUdDescendants(True, BI.path, BI, False, 1, 0, errorMessages)

            If sd.Any Then rfh = Join(sd.First.Value.hideReasonList.ToArray, ",")

            'if the search type is 'priced' then we only show products if there is no resont to hide them
            If searchtype = "priced" And rfh = "" Then  'Only show things that are in the feed file (although we searched everything)
                include = True
                'ElseIf searchtype = "stocked" Then ' loosley equivilent to 'feed only'

            ElseIf searchtype = "all" Then
                'if the search type is 'all' we show the product regardless of 'reasons for hide'
                include = True
            End If

            Dim price$ = ""
            Dim stock$ = ""

            Dim nv As Integer = -1

            If include Then ' include Then

                resultRow = New Panel
                resultRow.CssClass = "KWresultRow"
                outputResults.Controls.Add(resultRow)
                Form.Controls.Add(resultRow)
                output += 1

                If rfh <> "" Then  'these are reasons to hide this result (out of region, not in feed etc, EOL etc.)
                    Dim tt As Literal = New Literal
                    tt.Text = "<span title='" & rfh & "'>*</span>"
                    resultRow.Controls.Add(tt)
                    greyed = True
                Else
                    greyed = False
                End If

                Dim segs As Panel = KWbreadcrumbs(lid, "tree" & kvp.Key, language, isoptionsSearch, greyed, rfh, isDiagView) 'branch.keywords(language)
                resultRow.Controls.Add(segs)

                If branch.HasSKU Then
                    If NeedUpdate IsNot Nothing Then NeedUpdate.AddRange(branch.StalePrices(buyeraccount, errorMessages)) 'returns a set of variants
                    resultRow.Controls.Add(branch.BuyUI(buyeraccount, "tree" & kvp.Key, lid, True))
                End If

                'SCORE
                'segs.Controls.Add(NewLit("&nbsp;<b>" & kvp.Value & "</b>"))

                'If Not (searchtype = "stocked" And nv < 0) Then
                ' lit = New Literal
                ' lit.Text = "tree" & kvp.Key & "^" & kvp.Value & PathName$("tree" & kvp.Key, language) & sku$ & " " & price$ & " " & stock & "]"
                ' Form.Controls.Add(lit)
                'End If
            End If
            ' If maxOccurs = 1 Then Exit For  WTF ???? WASTED AN HOUR (or two!)
            If output > maxresults Then Exit For

        Next


        Dim footer As Panel = New Panel
        Form.Controls.Add(footer)

        If output = 0 Then
            lit = New Literal : lit.Text = "<p class='KWNoResults'>" & Xlt("No matching results", language) & "</p>"
            footer.Controls.Add(lit)
        ElseIf output > maxresults Then

            lit = New Literal : lit.Text = "<p class='KWNoResults'>" & Xlt("There are more results, please add keywords to refine your search", language) & "</p>"
            footer.Controls.Add(lit)


        ElseIf results.Count = 1000 Then

            lit = New Literal : lit.Text = "<p class='KWTooMany'>" & Xlt("Please add more search terms for better results", language) & "</p>"
            footer.Controls.Add(lit)

        End If


        If NeedUpdate IsNot Nothing AndAlso NeedUpdate.Count > 0 Then
            Dim handle As Integer
            handle = ModUniTran.DispatchUpdateRequest(lid, NeedUpdate, "", errorMessages)  'This issues a request to the Universal Translating webservice - and (instantly) returns a handle 

            'pbi.path$ - tree.1 was pbi.path - but there's no real reason (apart from perhaps swift) not to placeprices across the whole tree
            If handle = 0 Then
                errorMessages.Add("* Could not dispatch web request (handle was 0)")
            Else
                '"KWresultSet"
                'outputResults.Controls.Add(fetcherImage("KWresultSet", handle)) 'inserts an image with an onload script which calls the js FillPrices() after 5 seconds
                outputResults.Controls.Add(fetcherImage("KwResultsHolder", handle, NeedUpdate)) 'inserts an image with an onload script which calls the js FillPrices() after 5 seconds

            End If
        End If


    End Function

    ''' <summary>
    ''' Scores every Branch against the keywords (or fragments) - bear in mind branches appear in many places - see also clsBranch.Score
    ''' </summary>
    ''' <param name="frags"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ScoreBranches(frags As List(Of String), language As clsLanguage) As Dictionary(Of Integer, clsKwScore)

        Dim m As Integer  'bit mask (gets doubled and 'OR'd in)
        m = 1

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim viewAllRight = AccountHasRight(lid, "VIEWALL")

        Dim scores As Dictionary(Of Integer, clsKwScore)
        scores = New Dictionary(Of Integer, clsKwScore)(1000) 'size "estimate"

        Dim minorMatches As Integer = 0
        Dim majorMatches As Integer = 0

        Dim t! = Stopwatch.GetTimestamp

        Dim frag As String
        Dim cc As Integer = 0
        For Each frag In frags  'Frags are either indiviudal words (that were separated by spaces) or phrases (that were separtaed by commas) from the searchText
            frag = frag.ToLower
            If Len(frag) > 1 Then

                'use LINQ to quickly fetch all the branches that feature the keyword
                'a future optimisation might be to search the traslations only (and then return the branches that reference them)
                'Dim j = From branch In iq.Branches.Values Where LCase(branch.keywords(s_lang)).Contains(frag) And Not branch.Hidden  ' Or InStr(LCase(rr.keywords(s_lang)), kw) > 0

                '                Dim bids = iq.Branches.Keys.ToList()
                For Each branch In iq.Branches.Values ' bid In iq.Branches.Keys ' bids
                    'Dim branch = iq.Branches(bid)
                    If Not branch.unSearchable And Not branch.deleted Then  'For now, this has been used to supress SBSO 'Acceeories and services' - as a speed up

                        ' HPI/HPE split - make sure products from the other side aren't visible to the current agent (unless the user has the VIEWALL right)
                        If Not viewAllRight Then
                            If Not branch.Product Is Nothing Then
                                If branch.Product.Manufacturer <> agentAccount.Manufacturer Then Continue For
                            End If
                        End If

                        'NEW match by sku first
                        If branch.Product IsNot Nothing AndAlso branch.Product.SKU.Contains(frag) Then
                            If Not scores.ContainsKey(branch.ID) Then
                                scores.Add(branch.ID, New clsKwScore)
                            End If

                            With scores(branch.ID)
                                .majorMatchBits = .majorMatchBits Or m 'wether there is a match on this particular fragment (this may get set more than once)
                                .MajorMatchCount += 1                    'the total number of matches (of this fragment)
                                majorMatches += 1
                            End With

                        ElseIf branch.Majorkeywords(language).ToLower.Contains(frag) Then
                            If Not scores.ContainsKey(branch.ID) Then
                                scores.Add(branch.ID, New clsKwScore)
                            End If

                            With scores(branch.ID)
                                .majorMatchBits = .majorMatchBits Or m 'wether there is a match on this particular fragment (this may get set more than once)
                                .MajorMatchCount += 1                    'the total number of matches (of this fragment)
                                majorMatches += 1
                            End With
                        ElseIf branch.minorKeywords(language).ToLower.Contains(frag) Then
                            If Not scores.ContainsKey(branch.ID) Then
                                scores.Add(branch.ID, New clsKwScore)
                            End If

                            With scores(branch.ID)
                                .minorMatchbits = .minorMatchbits Or m 'wether there is a match on this particular fragment (this may get set more than once)
                                .MinorMatchCount += 1                    'the total number of matches (of this fragment)
                                minorMatches += 1
                            End With
                        End If

                        cc += 1
                        If cc = 100 Then
                            Dim ssid As Integer = iq.sesh(lid, "searchID")
                            If ssid <> searchID Then 'abandon
                                abandon = True
                                Return scores 'BAIL
                            End If
                            cc = 0
                        End If
                        'if the session variable searchID everty diverged from the local variable searchID - this (same) used has started another search - and this search can be abandoned.

                        '    If scores.Count > 1000 Then Exit For ' significant speedup/optimisation - at the cost that result sets for short fragments may not be entirely  'correct'
                    End If
                Next

                m = m + m  'Double the Bit mask (to address the next bit column in the Integer) - and the next fragment
            End If

        Next

        Dim et As Single = (Stopwatch.GetTimestamp - t!) / Stopwatch.Frequency * 1000

        Return scores  'Note Scores will ONLY contains values for scoring branches

    End Function

    Private Function segments(path$, ByRef segnum As Integer) As Array

        'pre-load the first part of the results with the treecursors' segments
        '(so we search only from the current point in the tree)
        Dim seg(50) As Integer

        For Each ss As String In Split(path$, ".")
            If ss <> "tree" Then
                seg(segnum) = CInt(ss)
            End If
            segnum += 1
        Next

        Return seg

    End Function
    ''' <summary>Splits the supplied searchtext into words and any comma seperated phrases</summary>
    Private Function Fragments(searchText As String) As List(Of String)

        Fragments = New List(Of String)
        'Break the search text into fragments (phrases are comma seperated)
        Dim c As Integer 'comma
        Dim s As Integer 'space
        If Right$(searchText, 1) <> " " Then searchText &= " "
        Do
            c = InStr(searchText, ",")
            If c > 0 Then
                Fragments.Add(Left(searchText, c - 1))
                searchText = Mid$(searchText, c + 1)
            Else
                s = InStr(searchText, " ")
                Fragments.Add(Left(searchText, s - 1))
                searchText = Mid$(searchText, s + 1)
            End If
        Loop Until searchText = ""

    End Function


    'Public Sub Junk()


    '    Dim j As List(Of Integer)
    '    j = New List(Of Integer)
    '    j.Add(23)
    '    j.Add(123)
    '    j.Add(223)
    '    j.Add(323)
    '    j.Add(5423)
    '    j.Add(5323)
    '    j.Add(243)
    '    j.Add(223)

    '    Dim t As Double
    '    Dim et As Double
    '    Dim frag
    '    Dim sql$



    '    t = Diagnostics.Stopwatch.GetTimestamp

    '    Dim scores As Dictionary(Of String, Integer)
    '    'iq.Root.score(tl, scores)


    '    et = (Diagnostics.Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000




    '    '      Dim sql$
    '    Sql$ = "SELECT id, count(*) as score FROM branch b join translation t on b.fk_translation_key = t.key translation where t.text like '" & frag & "%' and t.l;"  'OR - oR

    '    Sql$ = "SELECT"

    '    For Each branch In iq.Branches.Values
    '        If LCase(Left$(branch.Text(s_lang), Len(frag))) = frag Then

    '        End If
    '    Next

    '    If Len(frag) > 2 Then
    '        Dim path$
    '        path$ = iq.sesh(lid,"TreeCursor")

    '        'a product set is all the products that appear below a specific branch - eg. all 'notebooks' (and their options, options options etc)

    '        'See if we have a product set for this path cached.. otherwise, add one
    '        Dim productSet As List(Of clsProduct)

    '        With iq.PathCache  'this object contains a cache of recently/frequently used product sets by path, shared accross all user
    '            'each set is the distinct products that appear under the specified branch
    '            If .Sets.ContainsKey(path$) Then
    '                productSet = .Sets(path$)
    '            Else
    '                If .Sets.Count > 15 Then 'allow up to 15 cached sets - as the largest sets are kept, these will tend to be the BU's /large families
    '                    .removeSmallest() ' remove the smallest set (as it costs least to replace should we need to)
    '                End If
    '                productSet = .Add(path$)
    '            End If
    '        End With

    '        'terms enclosed in commas are ANDed, the remainder are OR'd
    '        Dim parts As List(Of String)
    '        parts = New List(Of String)

    '        '7.2k hdd,500gb  - searches for 7.2k hdd (as a term) OR 500gb
    '        'blue ray drive - searches for blue OR ray OR drive

    '        Dim c As Integer
    '        Dim s As Integer
    '        If Right$(frag, 1) <> " " Then frag &= " "

    '        Do
    '            c = InStr(frag, ",")
    '            If c Then
    '                parts.Add(Left(frag, c - 1))
    '                frag = Mid$(frag, c + 1)
    '            Else
    '                s = InStr(frag, " ")
    '                parts.Add(Left(frag, s - 1))
    '                frag = Mid$(frag, s + 1)
    '            End If
    '        Loop Until frag = ""

    '        Dim lit As Literal
    '        Dim matches As Integer = 0
    '        Dim score As Integer

    '        Dim results As Dictionary(Of clsProduct, Integer)
    '        results = New Dictionary(Of clsProduct, Integer)

    '        For Each p In productSet
    '            score = 0
    '            For Each a In p.Attributes.Values
    '                For Each kw In parts 'keyword
    '                    If InStr(LCase(iq.Translations(a.TextKey).text(s_lang)), kw) Then
    '                        score = score + 1
    '                    End If
    '                Next
    '            Next
    '            If score Then results.Add(p, score)
    '        Next

    '        'Use LINQ to return a sorted version of the dictionary (by descending value of score)
    '        Dim sorted = From rr In results Order By (rr.Value) Descending

    '        For Each kv In sorted  'iterate the key value (product, score)  pairs 
    '            lit = New Literal
    '            lit.Text = kv.Key.ID & "^" & kv.Value.ToString & " " & kv.Key.displayName(s_lang) & "]"
    '            Form.Controls.Add(lit)
    '            matches += 1
    '            If matches > 15 Then Exit For
    '        Next

    '        'sort the 'red list' - by matches withing the bracnh name
    '    End If




    'End Sub

End Class