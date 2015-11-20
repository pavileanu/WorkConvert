using IQ.clsBranchState;




/// <summary>
/// This class gathers info about the state of the branch from the users session - Keeping things neat and tidy (saving us passing many, many paramteres to  much of the rendering code)
/// </summary>
/// <remarks></remarks>

public class clsBranchInfo
{

    public UInt64 lid; //Log In Id (session key)
    public object path; //read-only          'path in the tree of this branch eg. 'tree.1.4.563.8383.3993
    public clsBranch branch;
    // Public branchState As clsBranchState 'Holds type,  numrows (to display) etc
    public clsAccount agentAccount;
    public clsAccount buyerAccount;
    public bool isTreeCursor;
    private clsScreenHeader _EffectiveHeader;
    public clsScreenHeader EffectiveHeader
    {
        get
        {
            if (ScreenHeader == null)
            {
                return _EffectiveHeader;
            }
            else
            {
                return ScreenHeader;
            }
        }
    }
    public clsScreenHeader ScreenHeader;


    public void CreateMatrixHeader(Dictionary<clsBranch, clsVisibility> descendants, bool United = false)
    {
        List<string> errorMessages = new List<string>();
        //Logic:
        Dictionary<string, clsScreenHeader> screenHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(this.lid, "screenHeaders"));
        if (screenHeaders == null)
        {
            screenHeaders = new Dictionary<string, clsScreenHeader>();
            iq.sesh(lid, "screenHeaders") = screenHeaders;
        }
        if (screenHeaders.ContainsKey(this.path))
        {
            this.ScreenHeader = screenHeaders[this.path];
        }
        else
        {
            //'        If rootPath IsNot Nothing Then
            // If matrixHeaders.ContainsKey(Me.rootPath) Then MatrixHeader = matrixHeaders(Me.rootPath)
            // Else
            // If matrixHeaders.ContainsKey(Me.path) Then MatrixHeader = matrixHeaders(Me.path)
            // End If
            //
            //Doesn't exist so we need to create one
            // If (Me.branch.Product Is Nothing) Then ' OrElse Not Me.branch.Product.isSystem) Then
            if (getBranchStateAbove(lid, path, errorMessages) != null)
            {
                if (getBranchStateAbove(lid, path, errorMessages).rca == enumBt.DetailSquare || getBranchStateAbove(lid, path, errorMessages).rca == enumBt.Branch)
                {
                    System.Int32 temp_skus = 0;
                    System.Int32 temp_cats = 0;
                    descendants = this.visibleChildren(errorMessages, United, ref temp_skus, ref temp_cats, true, true);
                    ScreenHeader = new clsScreenHeader(this, descendants, United);
                }
                else if (descendants != null)
                {
                    ScreenHeader = new clsScreenHeader(this, descendants, United);
                }
                else if (rootPath != null) // getBranchStateAbove(lid, path, errorMessages).rca = enumBt.gridrow Then
                {
                    _EffectiveHeader = matrixHeaderAbove(this.lid, rootPath == null ? path : rootPath, errorMessages); //provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)
                }
            }
            else
            {
                _EffectiveHeader = matrixHeaderAbove(this.lid, rootPath == null ? path : rootPath, errorMessages); //provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)
            }
            //End If
        }
    }
    public clsScreen EffectiveMatrix
    {
        get
        {
            return MatrixAbove(this.lid, this.path);
        }
    }
    public clsEditHeader editHeader;
    public Label lblMatches;
    public string rootPath;
    public bool showAll;
    public bool treeMode;
    public bool branchButtons; //Renders everything as a branch, and with extra buttons (prune, graft etc.)
    public float treeWidth;
    public string collectiveSingle; //How to refer to this branches lone child - eg 'Option','System','family'
    public string collectivePlural; //How to refer to this branches children (plural) - eg 'Options','Systems','families'
    public HashSet<string> foci; //Addional filtering of products by their focus attribute (Receta being the prime example)
    public int survivors; //number of rows 'suriving' the 'Quick' filtering
    public enumParadigm Paradigm;
    public int rownum;
    public string divToFill; //used by processcommand to return the DivToFill (either 'path' or "tree" (for OpenTO's)
    public List<string> userMessages; //Used to conver swift and shopping list warnings/exceptions

    public void setQuickFiltersVisible(bool visible)
    {

        Dictionary<string, clsScreenHeader> matrixHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(this.lid, "screenHeaders"));
        if (!matrixHeaders.ContainsKey(this.path))
        {
            object bs = getbranchstate(this.lid, this.path);
            List<string> errorMessages = new List<string>();
            System.Int32 temp_skus = 0;
            System.Int32 temp_cats = 0;
            CreateMatrixHeader(visibleChildren(errorMessages, System.Convert.ToBoolean(bs.United), ref temp_skus, ref temp_cats, true, true));
        }

        matrixHeaders[this.path].QuickFiltersVisible = visible;
        //EffectiveHeader.QuickFiltersVisible = visible
    }

    public clsBranchInfo(UInt64 lid, string path)
    {
        //super lightweight version used by keywordsearch  (outputresults)

        this.lid = lid;
        this.path = path;

        this.showAll = System.Convert.ToBoolean(iq.sesh(lid, "showAll"));
        this.agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
        this.buyerAccount = (clsAccount)(iq.sesh(lid, "BuyerAccount"));
        this.foci = new HashSet<string>(Strings.Split(System.Convert.ToString(iq.sesh(this.lid, "foci")), ",").ToList);
        this.userMessages = new List<string>();
    }

    public clsBranchInfo(UInt64 lid, object path, Label lblMatches, float treewidth, enumParadigm Paradigm, List<string> errorMessages, string rootPath = null)
    {

        //Much of the BranchInfo is pulled from the session variables
        this.lid = lid;
        this.path = path;
        this.Paradigm = Paradigm;
        this.rootPath = rootPath;

        clsBranchState branchstate = getbranchstate(this.lid, this.path);

        //If (branchstate IsNot Nothing AndAlso branchstate.rca = enumBt.Tab) Then treewidth = treewidth - CType(1, Single) Else treewidth = treewidth - CType(2.25, Single)

        //check required for editor (at the moment)
        if (Information.IsNumeric(Strings.Split(System.Convert.ToString(path), ".").Last))
        {
            this.branch = iq.Branches(System.Convert.ToInt32(Strings.Split(System.Convert.ToString(path), ".").Last));
        }

        this.showAll = System.Convert.ToBoolean(iq.sesh(lid, "showAll"));
        this.treeMode = System.Convert.ToBoolean(iq.sesh(lid, "treeMode"));

        //If Me.ShowAll Then Stop
        this.branchButtons = System.Convert.ToBoolean(iq.sesh(lid, "branchButtons")); //

        //build a hashset from the CD list stored in the session variable
        this.foci = new HashSet<string>(Strings.Split(System.Convert.ToString(iq.sesh(this.lid, "foci")), ",").ToList);


        this.lblMatches = lblMatches;
        this.agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
        this.buyerAccount = (clsAccount)(iq.sesh(lid, "BuyerAccount"));

        CreateMatrixHeader(null);
        //'Any row rendering its chidren as gridrows will have a matrixheader
        //Me.MatrixHeader = Nothing

        //Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
        //If matrixHeaders Is Nothing Then
        //    matrixHeaders = New Dictionary(Of String, clsScreenHeader) : iq.sesh(lid, "matrixHeaders") = matrixHeaders
        //End If

        //'ML - Added rootPath so that the screen definition cannot be different for children of the currently displayed screen
        //If branchstate IsNot Nothing AndAlso branchstate.United = True AndAlso rootPath IsNot Nothing Then
        //    If Not matrixHeaders.ContainsKey(Me.path) AndAlso matrixHeaders.ContainsKey(rootPath) Then
        //        Me.matrixHeader = matrixHeaders(rootPath).Clone()
        //        matrixHeaders.Add(Me.path, Me.matrixHeader)
        //    End If
        //End If
        //If matrixHeader Is Nothing Then
        //    If matrixHeaders.ContainsKey(Me.path) Then
        //        Me.matrixHeader = matrixHeaders(Me.path)
        //    Else
        //        'Lets create a matrix here using the data from anything above

        //    End If
        //End If

        //Me.matrixHeader = matrixHeaderAbove(lid, If(rootPath Is Nothing, path$, rootPath), errorMessages) 'provides a reference to the clsScreenHeader in force for this branch - for this user-session (for formatting)

        // If Me.effectiveHeader Is Nothing Then Stop

        if (this.ScreenHeader != null && this.ScreenHeader.Vw != null)
        {
            this.survivors = System.Convert.ToInt32(this.ScreenHeader.Vw.Count);
            // If Me.Survivors = 0 Then Stop
        }


        //      If Me.Survivors = 0 Then Stop 'remove

        //   '@@@ DELEE

        //If Me.ShowAll Then
        //    'when switching to 'showall' mode - it's possible that we're revealing siblings that have never been rendered - as such they don't have state
        //    Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        //    If Not branchStates.ContainsKey(Me.path) Then
        //        If branchStates.ContainsKey(oneAbove(Me.path)) Then
        //            branchStates.Add (me.path, New clsBranchState( ) ' create a (closed) branch like the first visible sibiling
        //        End If
        //    End If

        //  Me.branchState = getbranchstate(Me) 'returns the branchState at this path IF it Exists - It's Legitimate for htis to return NOTHING for a branch we don't want to render

        if (iq.SeshContains(lid, "treeCursorPath"))
        {
            if (Strings.LCase(System.Convert.ToString(path)) == (iq.sesh(lid, "treeCursorPath")).ToString())
            {
                this.isTreeCursor = true;
            }
        }

        string[] p = Strings.Split(System.Convert.ToString(path), ".");

        if (!(this.agentAccount == null))
        {
            if (p.Count() > 0)
            {
                clsBranch parentbranch = iq.Branches(System.Convert.ToInt32(p.Last));
                this.collectiveSingle = System.Convert.ToString(parentbranch.collectiveNounSingular.text(this.agentAccount.Language)); //get the name of the things we're viewing from the first one of them (e.g. servers)
                this.collectivePlural = System.Convert.ToString(parentbranch.CollectiveNoun.text(this.agentAccount.Language));
            }
            else
            {
                this.collectiveSingle = System.Convert.ToString(iq.RootBranch.collectiveNounSingular.text(this.agentAccount.Language)); //get the name of the things we're viewing from the first one of them (e.g. servers)
                this.collectivePlural = System.Convert.ToString(iq.RootBranch.CollectiveNoun.text(this.agentAccount.Language));
            }
        }

        this.branchButtons = branchButtons;
        this.treeWidth = treewidth;
        this.userMessages = new List<string>();


    }

    public bool MoreThanXskus(int max)
    {

        List<string> errorMessages = new List<string>();
        int found = 0;
        this.branch.getSKUdDescendants(false, this.path, this, false, max, found, errorMessages);

        if (found >= max)
        {
            return true;
        }

    }

    /// <summary>Fetch either the direct descendants (which may be categories) or the SKUD descendants (products) </summary>
    /// <param name="errorMessages"></param>
    /// <param name="united"></param>
    /// <param name="skus"></param>
    /// <param name="cats"></param>
    /// <param name="fbv">whether to filter and sort (the descenants) by the view (i.e. filters) in force</param>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public Dictionary<clsBranch, clsVisibility> visibleChildren(List<string> errorMessages, bool united, ref int skus, ref int cats, bool fbv, bool filter, bool checkSystem = false)
    {

        Dictionary<clsBranch, clsVisibility> vc = default(Dictionary<clsBranch, clsVisibility>);

        if (united)
        {
            if (checkSystem)
            {
                vc = this.branch.getSKUdDescendants(false, this.path, this, this.branch.Product.isSystem(), 10000, 0, errorMessages);
            }
            else
            {
                vc = this.branch.getSKUdDescendants(false, this.path, this, false, 10000, 0, errorMessages);
            }
            //these might be options - or systems
            cats = 0;
            skus = System.Convert.ToInt32(vc.Count);
        }
        else
        {
            vc = this.branch.getVisibleChildren(this, errorMessages, skus, cats);
            //    If vc.Count = 0 Then Stop

        }

        if (((enumParadigm)(iq.sesh(lid, "Paradigm"))) == enumParadigm.configuringSystem && !isSystemAbove(lid, path))
        {
            List<clsBranch> vcremove = new List<clsBranch>();
            foreach (var v in vc)
            {
                if (((Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"))).ContainsKey(v.Value.path) && ((Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates")))(v.Value.path) != null)
                {
                }
                else
                {
                    vcremove.Add(v.Key);
                }
            }
            foreach (var k in vcremove)
            {
                vc.Remove(k);
            }
        }


        //the root view may not be populated
        if (this.ScreenHeader != null)
        {
            if (this.ScreenHeader.Vw == null)
            {
                return vc;
            }
        }

        if (filter == false)
        {
            return vc; //important
        }

        clsScreenHeader eh = null; //When recalcualting filters - we need to NOT apply the view at this level - but DO apply upper filters in effect
        string ehp = System.Convert.ToString(this.path); //effective header path
        if (fbv == false)
        {
            ehp = ehp.Substring(0, Strings.InStrRev(ehp, ".") - 1); //filter by view
        }
        if (ehp == "tree")
        {
            return vc;
        }

        eh = EffectiveHeader; //matrixHeaderAbove(Me.lid, ehp, errorMessages)

        if (fbv && eh != null && eh.Vw != null && eh.Vw.Count > 0)
        {
            eh.RefreshViewFilter();
            Dictionary<clsBranch, clsVisibility> filteredAndSorted = new Dictionary<clsBranch, clsVisibility>();

            //If the visibleChildren have SKUs they're products, otherwise they're categories
            //**There can never be a mix ! **
            if (vc.Values.Count > 0 && vc.Values.First.branch.HasSKU)
            {
                //products
                //we iterate over the view - becuase it provides the sort
                int cc = System.Convert.ToInt32(eh.Vw.Count);
                for (int i = 0; i <= eh.Vw.Count - 1; i++)
                {
                    clsBranch branch = iq.Branches(System.Convert.ToInt32(eh.Vw(i).Item("id")));
                    object pn = branch.Product.DisplayName(English);

                    //There ARE some things in the view - which have now been determined as not visible
                    if (vc.ContainsKey(branch))
                    {
                        filteredAndSorted.Add(branch, vc[branch]);
                    }
                    else
                    {
                        //                            Beep()
                    }
                }
            }
            else
            {
                //categories - these aren't in the view.. but are in VC
                //Temporarily change sort of table for the .find function which MUST be by ID
                object s = eh.Vw.Sort;
                eh.Vw.Sort = "[ID]";
                foreach (var vis in vc.Values.OrderBy(c => c.branch.order)) //vc are the visible children (clsVisibility)  - they may be SKU'd product branches or SKUless category branches
                {
                    if (vis.branch.isInOrHasDescendantIn(eh.Vw) || showAll)
                    {
                        filteredAndSorted.Add(vis.branch, vis);
                    }
                }
                eh.Vw.Sort = s;
            }

            return filteredAndSorted;

        }
        else
        {
            return vc;
        }


    }

    public void switchTo(enumBt bt, ref List<string> errorMessages)
    {

        clsBranchState branchstate = getbranchstate(this.lid, this.path);

        // Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        //branchState = branchStates(Me.path)

        if (branchstate == null)
        {
            errorMessages.Add("branchstate was nothing for path me.path in switchto ");
        }
        else
        {
            branchstate.rca = bt;

            //We already have state for this branch so it must be a view switch
            //IF there is an explict type specified as a paramater - (ie, we used the switcher)
            if (bt == enumBt.errorNotSet)
            {
                Interaction.Beep();
            }

            if (bt == enumBt.DetailSquare)
            {
                if (!(branchstate.United == false))
                {
                    InvalidateMatrixBelow(System.Convert.ToString(this.path));
                }

                System.Int32 temp_skus = 0;
                System.Int32 temp_cats = 0;
                Dictionary<clsBranch, clsVisibility> descendants = this.visibleChildren(errorMessages, true, ref temp_skus, ref temp_cats, true, true);
                branchstate.rca = enumBt.DetailSquare; // Ok - now Render your Children As detailed squares

                this.CreateMatrixHeader(descendants, false);
                branchstate.United = false;
            }
            else if (bt == enumBt.gridrow)
            {
                if (!(branchstate.United == true))
                {
                    InvalidateMatrixBelow(System.Convert.ToString(this.path));
                }
                branchstate.United = true;

                CloseBelow(this.lid, this.path); //this is really important - we need to kill all state below the branch we're swithing to a grid (otherwise (for example) supply chains will cause gridrows to render as branches - see GetBranchstateAbove)

                //potential very large speedup here - by checking whether the matrixHeader and its associated datatable/view already exits
                //(in the sesh)
                //we would not then need to fetch the descendants or rebuild the matrixheader

                //Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(Me.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
                //If (matrixHeaders.ContainsKey(Me.path)) Then
                //    Me.MatrixHeader = matrixHeaders(Me.path)
                //Else

                System.Int32 temp_skus2 = 0;
                System.Int32 temp_cats2 = 0;
                Dictionary<clsBranch, clsVisibility> descendants = this.visibleChildren(errorMessages, true, ref temp_skus2, ref temp_cats2, true, true);

                branchstate.rca = enumBt.gridrow; // Ok - now Render your Children As gridrows

                this.CreateMatrixHeader(descendants, false);

                //Me.switchToGrid(descendants, errorMessages) 'This is really important
                //       Me.MatrixHeader.rebuild(Me, descendants, errorMessages) 'became..
                //End If
            }
            else
            {
                if (!(branchstate.United == false))
                {
                    InvalidateMatrixBelow(System.Convert.ToString(this.path));
                }
                branchstate.United = false;
            }
        }

    }

    public void InvalidateMatrixBelow(string path, bool IncludeActualPath = false)
    {
        Dictionary<string, clsScreenHeader> matrixHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(lid, "screenHeaders"));
        foreach (string s in matrixHeaders.Keys.ToList().Where(a => a.StartsWith(path) && (IncludeActualPath || a != path)))
        {
            matrixHeaders.Remove(s);
        }
    }

    //Public Function QuoteContains(product As clsProduct) As Boolean


    //    If iq.SeshContains(Me.lid, "QuoteID") Then
    //        Dim quoteID As Integer
    //        quoteID = CInt(iq.sesh(lid, "QuoteID"))
    //        If quoteID <> 0 Then
    //            Dim quote As clsQuote = CType(iq.sesh(Me.lid, "AgentAccount"), clsAccount).Quotes(quoteID)

    //            Dim j = F-rom i In quote.RootItem.Flattened(False, False, 0).items Where i.QuoteItem.Branch.Product Is product
    //            If j.Any Then QuoteContains = True

    //        End If
    //    End If

    //End Function



    public void close(List<string> errorMessages)
    {

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));
        branchStates.Remove(this.path);
        //branchStates(Me.path).rca = enumBt.Hidden  'we KEEP the state, and set its RCA as hidden (this is a bit like a 'visited' hyperlink) - if we were to delete the state, we have no way to store closed branches (specifically to mix opened and closed branches at any given level)

    }


    public clsBranchState open(ref List<string> errorMessages, bool openFirstTab) //BranchState is a RERERENCE to an object utimately stored under sesh(lid)
    {

        //open this branch - and set how it renders its children based upon the type depth, existing state of the parent (being opened)

        clsBranchState branchstate = getbranchstate(this.lid, this.path);

        //Just In Time Carepacks
        if (this.branch.HasSKU && this.branch.Product.isSystem)
        {
            // Me.Branch.createCarePacks(Me.path, Me.BuyerAccount)
        }

        //If Me.path = "tree.1" Then Stop


        if (branchstate == null || branchstate.rca == enumBt.Hidden)
        {
            if (branch.rca == "")
            {
                errorMessages.Add("RCA for Branch " + this.branch.ID.ToString() + " was not set defaulting to B");
                branch.rca = "B";
            }

            bool unite = branch.rca.First == "G";
            enumBt bt = default(enumBt);
            bt = (enumBt)(BTchar.IndexOf(this.branch.rca.First)); //use the default form from the branches RCA
            if (branchstate == null)
            {
                branchstate = new clsBranchState(this.lid, this.path, bt, unite, this.rownum, 100);
            }
            else
            {
                branchstate.rca = bt;
                branchstate.United = unite;
            }
        }

        //this is the Adding Systems 'Paradigm' - we hide the options (f*cking insanity - but ours is not to reason why)
        //put that another way - we only show the option tabs on a system when we have one of them in the basket
        //(WHY they couldn't just be collapsed - i don't know)
        //    If Me.Paradigm = enumParadigm.AddingSystem And Me.Branch.Product IsNot Nothing AndAlso Me.Branch.Product.isSystem Then
        //If Not Me.QuoteContains(Me.Branch.Product) Then
        // branchstate.rca = enumBt.Hidden
        // End If

        if (branchstate.rca == enumBt.gridrow || branchstate.rca == enumBt.Tab)
        {
            //If Branch.childBranches.Count = 0 Then Stop
            bool fbv = false;
            if (treeMode)
            {
                fbv = false;
            }
            System.Int32 temp_skus = 0;
            System.Int32 temp_cats = 0;
            Dictionary<clsBranch, clsVisibility> descendants = this.visibleChildren(errorMessages, System.Convert.ToBoolean(branchstate.United), ref temp_skus, ref temp_cats, fbv, true);

            //If descendants.Count = 0 Then Stop

            //It is Legitimate for a Branch with no children to be open(ed) .. They render differenty (eg. options may dispay their attributes when open)
            //  If descendants.Count = 0 Then Stop

            if (branchstate.rca == enumBt.gridrow)
            {

                //If we're opening up a grid... we need to create a matrixheader
                if (descendants.Count > 0 && this.ScreenHeader == null) //NOT at the level of an option (with no descendants) though - or bad things happen
                {
                    Dictionary<string, clsScreenHeader> matrixHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(lid, "screenHeaders"));
                    if (!matrixHeaders.ContainsKey(this.path))
                    {
                        CreateMatrixHeader(descendants, false);

                        //Me.MatrixHeader = mh 'Added 150914 ML, header was not populating on refresh as it was never assigned to this branch info although it was added to the sesh
                    }
                }

            }
            else if (branchstate.rca == enumBt.Tab && openFirstTab)
            {
                //I'm rendering my children as tabs - autoOpen the first one
                //autoOpenFirstTab()

                bool tabOpen = false;
                clsBranchInfo firstTab = null;
                foreach (var vi in descendants.Values) //From j In descendants.Values Order By iq.Branches(CInt(Split(j.path, ".").Last)).order
                {
                    clsBranchState tbs = getbranchstate(this.lid, vi.path);
                    if (firstTab == null)
                    {
                        firstTab = new clsBranchInfo(this.lid, vi.path, null, this.treeWidth, this.Paradigm, errorMessages);
                    }

                    if (tbs != null && tbs.rca != enumBt.Hidden)
                    {
                        tabOpen = true;
                        break;
                    }
                }
                if (!tabOpen)
                {
                    if (firstTab != null) //clicking the hyperlink twice do descendants here
                    {
                        firstTab.open(ref errorMessages, openFirstTab); //if none of the tabs in this set are open - we open the first (actually this just sets state - it will be rendered open)
                    }
                }
            }
        }


        //@@ moved from Processcommand - becuase branches are also .opened from ploughpath
        if (branchstate != null)
        {
            if (branchstate.rca == enumBt.OpenBranch) //AUTOOPEN - 'O' type branches
            {
                foreach (var CB in this.branch.childBranches.Values)
                {
                    clsBranchInfo NBI = new clsBranchInfo(this.lid, this.path + "." + CB.ID, null, this.treeWidth, this.Paradigm, errorMessages);
                    NBI.open(ref errorMessages, false);
                }
            }
        }


        return branchstate;

    }

    //Public Sub prepareGridView(ByRef errormessages As List(Of String))

    //    'work out the availablewidth emsAvailable
    //    Dim pth As String = ""
    //    Dim w As Single = Me.treeWidth  'The full width of the left pane

    //    w -= 1  'A little bit of breathing space

    //    For Each seg In Me.path.Split(".".ToArray)
    //        pth &= seg

    //        If pth <> "tree" Then
    //            Dim bs As clsBranchState = getbranchstate(Me.lid, pth)
    //            If bs IsNot Nothing Then  'we may cross some branches that have never been rendered (they are united) - hence have no state
    //                Select Case bs.rca
    //                    Case Is = enumBt.OpenSquare 'BreadCrumb
    //                        w = Me.treeWidth
    //                    Case Is = enumBt.Branch
    //                        w = CSng(w - 2.25) 'ems indent 'was 2.5
    //                    Case Is = enumBt.Tab, enumBt.gridrow
    //                        '   w = CSng(w - 1) 'ems indent 'was 2.5
    //                    Case Else
    //                        '   w = CSng(w - 2.25) '- this causes otpions squishing
    //                        '  Beep()

    //                End Select

    //                If Paradigm = enumParadigm.configuringSystem And iq.Branches(CInt(seg)).hassystem Then w = treeWidth

    //            End If
    //        End If

    //        pth &= "."
    //    Next

    //    Me.MatrixHeader.CollapseColumns(w, errormessages)

    //    ''we MUST kill deeper matrixheaders (otherwise individual 'united' rows can render with the wrong header set - this will need to change
    //    'Dim tokill As List(Of String) = New List(Of String)
    //    'For Each p In matrixHeaders.Keys.ToArray
    //    '    If plen(p) > plen(Me.path) Then
    //    '        If Left$(p, Len(Me.path$)) = Me.path Then
    //    '            matrixHeaders.Remove(p)
    //    '        End If
    //    '    End If
    //    'Next p

    //End Sub

    public int PathLevel()
    {
        return path.Split('.').Length - 1;
    }


}


//        Return branchstate

//Else
//Beep()

//    If CType(iq.sesh(lid, "treeMode"), Boolean) = True Then
//        bs = New clsBranchState(Me.lid, Me.path, bt.Branch, False, 0, Me.rownum, 1000)
//        Return Me
//    End If

//    If Me.Branch.Picture = "hptop" Then
//        bs = New clsBranchState(Me.lid, Me.path, bt.TROhead, False, 0, Me.rownum, 1000)
//        Return Me
//    End If

//    'If .rca = bt.TROhead Then
//    ' .rca = bt.TROitem
//    ' Return Me
//    ' End If

//    '   If .rca = bt.BreadCrumb Then  'clicked on a breadcrumb
//    '.state = oc.open
//    'setChildBranches(oc.closed, bt.Square, OpenWhich.None, errorMessages) 'and render my children squares
//    ' Else
//    'Dim openWhich As OpenWhich = openWhich.None
//    'Dim openAs As bt = .renderAs 'What we will open the children as .. Inherit this branches type by default  . .

//    Dim parentPath As String = ""
//    Dim wasSquare As Boolean = False
//    'Special handling for opening a branch that is currently a square..


//.United = False
//        'wasSquare = True
//    ElseIf pbs.rca = bt.Tab Then

//        'autoOpen the first tab only if there is no state
//        If getbranchstate(lid, Me.path) Is Nothing Then
//            openWhich = openWhich.First
//        End If
//    End If

//    .state = oc.open  'open me

//    'Tim mode (opening a family) - opens the supply chains - shows all models as branches
//    If seg.Length = 4 Then
//        bs = New clsBranchState(Me.lid, Me.path, bt.Branch, False, 0, Me.rownum, 1000)

//        'If .renderAs = bt.errorNotSet Then
//        openAs = bt.Branch
//        ' End If
//        .United = False
//        .renderAs = bt.Branch

//        'Nick Mode
//        ' openWhich = openWhich.None : .United = True : openAs = bt.gridrow
//    End If

//    If Me.Branch.Product IsNot Nothing Then
//        If Me.Branch.Product.isSystem Then
//            ' .renderAs = bt.Branch

//            If Paradigm = enumParadigm.AddingSystem Then
//                .openWhich = openWhich.None 'for greg (STUPID idea but who am I to argue)
//                .rca = bt.hidden
//            ElseIf Paradigm = enumParadigm.configuringSystem Then
//                .rca = bt.Tab
//                'If stateBelow(Me.lid, path) Then
//                '    openWhich = openWhich.None 'if a tab is already open - don't reopen the first
//                'Else
//                '    openWhich = openWhich.First

//                'End If
//                '                            .rcenderAs = bt.headless
//            Else
//                Beep()
//            End If

//            .United = False
//        End If
//    End If

//    'new for dan
//    If seg.Length = 8 Then
//        .rca = bt.Branch
//        .United = False
//        .openWhich = openWhich.None
//    End If

//    Dim descendants As Dictionary(Of clsBranch, clsVisibility) ' = Me.visiblechildren(setChildBranches(oc.closed, openAs, openWhich, errorMessages)

//    If seg.Length = 9 Then
//        .rca = bt.gridrow  'Show grids inside the nested option tabs
//        .United = True
//        .openWhich = openWhich.None
//        descendants = Me.visiblechildren(errorMessages) 'setChildBranches(oc.closed, openAs, openWhich, errorMessages)
//        Me.switchToGrid(descendants, errorMessages) 'Creates a set of grid headers (holding filters, sorts, column widths etc.) for this user session
//    End If


//    'generally we come through here ..
//    ' Me.setChildBranches(oc.closed, openAs, openWhich, errorMessages)

//    'for squares - we must force it to re-render from the parent
//    If wasSquare Then Return New clsBranchInfo(Me.lid, parentPath, Nothing, Me.treeWidth, Me.Paradigm, errorMessages)

//End If
//End With
//End If

//Return Me 'this is VITAL (to return some branchinfo)