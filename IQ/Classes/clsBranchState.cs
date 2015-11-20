using System.Threading;

//Option Strict On
[Serializable]
public class clsBranchState
{

    //    Public renderAs As bt
    //Public state As oc  'Open or closed
    public int maxChildren; //for grids - how many rows to display (show all rows button)
    public int rownum; // used if/when rendering alternatind rows (matric row odd, matric row even)
    public bool United; //Flatten this branch - (render all the SKUD descendedents)
    public enumBt rca; //Descendants render as 'the current' switcher mode of this branch - strictly redundant - but saves alot of looking up of the state ofr first descendants etc
    //Public openWhich As OpenWhich
    public DateTime timestamp;


    public static object aLock = new object();

    public clsBranchState()
    {

    }


    /// <summary>
    /// returns the branch state (from the session) - United,OpenWhich, RCA etc..
    /// </summary>
    /// <param name="lid"></param>
    /// <param name="path"></param>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
    public static clsBranchState get_getbranchstate(UInt64 lid, string path)
    {
        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));
        if (!branchStates.ContainsKey(path))
        {
            return null;
        }
        else
        {
            return branchStates[path]; // branchStates(bi.path)
        }
    }


    public static void setBranchState(UInt64 lid, string path, clsBranchState bs)
    {

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));
        branchStates[path] = bs; // branchStates(bi.path)

    }


    public static clsBranchState get_getBranchStateAbove(UInt64 lid, string path, List<string> errormessages)
    {

        if (get_getbranchstate(lid, "tree") == null)
        {
            clsBranchState aboveroot = new clsBranchState(lid, "tree", enumBt.OpenSquare, false, 0, 100); //the 'All products' renders as a breadcrumb
        }


        if (path == "")
        {
            errormessages.Add("Path was blank in getBranchStateAbove");
            return null;
        }
        else
        {
            if (path.IndexOf(".") + 1 == 0)
            {
                errormessages.Add("Path contained no . in getBranchStateAbove");
                return null;
            }
            else
            {
                do
                {
                    path = path.Substring(0, Strings.InStrRev(path, ".") - 1);
                    clsBranchState bs = get_getbranchstate(lid, path);
                    if (bs != null)
                    {
                        return bs;
                    }

                    if (path.IndexOf(".") + 1 == 0)
                    {
                        errormessages.Add("No dots left in getBranchStateAbove");
                        return null;
                    }

                } while (true);

            }

        }

    }



    //Public Shared ReadOnly Property branchState(bi As clsBranchInfo) As clsBranchState 'Sub setState(state As oc, renderAs As bt)

    //    Get
    //        'Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(bi.lid, "branchStates")
    //        'If branchStates Is Nothing Then Stop
    //        Return iq.sesh(bi.lid, "branchStates")(bi.path) ' branchStates(bi.path)
    //    End Get
    //End Property

    public clsBranchState(UInt64 lid, object path, enumBt rca, bool unite, int rownum, int maxchildren)
    {

        this.rca = rca;
        this.rownum = rownum;
        this.maxChildren = maxchildren;
        this.United = unite;

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));

        if (!branchStates.ContainsKey(path))
        {
            branchStates.Add(path, this);
        }
        else
        {
            branchStates[path] = this; //happens when we autoopen a branch already autoopened
        }
        this.timestamp = DateTime.Now;


    }

    public static dynamic removeBranchState(UInt64 lid, object path)
    {

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));
        branchStates.Remove(path);

    }

    public static void HideSiblings(UInt64 lid, object path)
    {

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));

        foreach (var p in branchStates.Keys.ToArray)
        {
            if (plen(p) == plen(path) && p != path)
            {
                branchStates[p].rca = enumBt.Hidden;
            }
        }
    }

    public static int plen(object path)
    {
        int returnValue = 0;
        returnValue = Strings.Split(System.Convert.ToString(path), ".").Count();

        return returnValue;
    }
    public static void closeBranchesNotOnPath(UInt64 lid, object path)
    {

        //Sets the sesh variables affecting the tree rendering
        //                                                                                                           Path
        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));

        foreach (var p in branchStates.Keys.ToArray)
        {
            if (plen(p) > plen(path))
            {
                //this is below the path
                branchStates.Remove(p);
            }
            else
            {
                if (p != Strings.Left(System.Convert.ToString(path), Strings.Len(p)))
                {
                    branchStates.Remove(p);
                }
            }
        }

    }


    public static void CloseBelow(UInt64 lid, object path)
    {

        //during a resize - this gets called by multiple threads .. and it can remove a branchstate twice
        //the synclock may or may not 'solve' the problem - but it would nice to fully understand the root cause

        lock (aLock)
        {
            //Sets the sesh variables affecting the tree rendering
            //                                                                                                           Path
            //Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
            Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates"); //;, Dictionary(Of String, clsBranchState)

            //branchStates.Clear() TESTING

            foreach (var p in branchStates.Keys.ToArray)
            {
                if (plen(p) > plen(path))
                {

                    //branchStates(p).rca = enumBt.Hidden
                    branchStates.Remove(p);

                }

                if (plen(p) == plen(path) && branchStates[p].rca == enumBt.OpenSquare)
                {
                    branchStates.Remove(p);
                }
            }
        }

    }
    public static void CloseSiblings(UInt64 lid, string path)
    {
        Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates"); //;, Dictionary(Of String, clsBranchState)

        //branchStates.Clear() TESTING
        string pth = "";
        if (path.Split('.').Length > 1)
        {
            pth = path.Substring(0, path.Length - path.Split('.').Last.Length);
        }

        foreach (var p in branchStates.Keys.ToList().Where(f => f.StartsWith(pth)))
        {
            //If branchStates(p).rca = enumBt.OpenBranch Then
            if (p != path)
            {
                branchStates[p].rca = enumBt.Hidden;
            }
            // End If
        }
    }
    public static void CloseAbove(UInt64 lid, object path)
    {

        //Sets the sesh variables affecting the tree rendering
        //                                                                                                           Path
        //Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
        Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates"); //;, Dictionary(Of String, clsBranchState)

        //branchStates.Clear() TESTING

        foreach (var p in branchStates.Keys.ToArray)
        {
            if (plen(p) <= plen(path) && (branchStates[p].rca == enumBt.DetailSquare || branchStates[p].rca == enumBt.Square))
            {

                branchStates[p].rca = enumBt.OpenSquare;
                // branchStates.Remove(p)

            }
        }

    }

    /// <summary>Returns the subsection of the path down to (and including) the system unit (if present)</summary>
    public static string pathToSystem(object path)
    {

        string pth = "";
        foreach (var seg in Strings.Split(System.Convert.ToString(path), "."))
        {
            pth += System.Convert.ToString(seg);
            if (seg != "tree")
            {
                if (iq.Branches(System.Convert.ToInt32(seg)).Product != null)
                {
                    if (iq.Branches(System.Convert.ToInt32(seg)).Product.isSystem)
                    {
                        break;
                    }
                }
            }
            pth += ".";
        }

        if (pth.EndsWith("."))
        {
            pth = pth.Substring(0, pth.Length - 1);
        }
        return pth;

    }

    public static bool HasSystem(object path)
    {
        bool returnValue = false;

        returnValue = false;
        foreach (var seg in Strings.Split(System.Convert.ToString(path), "."))
        {
            if (seg != "tree")
            {
                if (iq.Branches(System.Convert.ToInt32(seg)).Product != null)
                {
                    if (iq.Branches(System.Convert.ToInt32(seg)).Product.isSystem)
                    {
                        return true;
                    }
                }
            }
        }
        return returnValue;
    }


    public static void PloughPath(UInt64 lid, object path, List<string> errorMessages, float treewidth, enumParadigm paradigm) //, treewidth As Single)
    {

        //Sets the sesh variables to Open all the branches on the path appropriately for the 'show in tree' function

        object pp;
        string pth = "tree";

        string[] segs = Strings.Split(System.Convert.ToString(path), ".");
        //Dim hitSystem As Boolean = False

        //Dim lastButOne = segs(UBound(segs) - 1)
        foreach (var seg in segs)
        {
            if (seg != "tree")
            {

                pp = pth;
                pth += "." + seg;

                //we want to keep state if it's there - and if not use .open to make it
                clsBranchState bs = get_getbranchstate(lid, pth);
                clsBranchInfo bi = new clsBranchInfo(lid, pth, null, treewidth, paradigm, errorMessages);

                // If bs Is Nothing Then
                bs = bi.open(errorMessages, false); //this branch has not yet been opened (rendered)
                //*KW If bs.rca = enumBt.Square Or bs.rca = enumBt.DetailSquare Then CloseAbove(lid, pth$)
                if (((enumParadigm)(iq.sesh(lid, "Paradigm"))) == enumParadigm.configuringSystem)
                {
                    CloseSiblings(lid, pth);
                }
                //End If
                //If bs.rca = enumBt.Square Or bs.rca = enumBt.DetailSquare Then
                // bs.rca = enumBt.OpenSquare
                //    CloseAbove(bi.lid, bi.path)
                //End If
                // If bs.rca = enumBt.Branch Then bs.rca = enumBt.OpenBranch
            }
        }

    }





}