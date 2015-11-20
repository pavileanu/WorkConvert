//Option Strict On
[Serializable()]
public class clsBranchState
{

	//    Public renderAs As bt
	//Public state As oc  'Open or closed
		//for grids - how many rows to display (show all rows button)
	public int maxChildren;
		// used if/when rendering alternatind rows (matric row odd, matric row even)
	public int rownum;
		//Flatten this branch - (render all the SKUD descendedents)
	public bool United;
		//Descendants render as 'the current' switcher mode of this branch - strictly redundant - but saves alot of looking up of the state ofr first descendants etc
	public enumBt rca;
	//Public openWhich As OpenWhich

	public DateTime timestamp;


	public static  aLock = new object();

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
	public static clsBranchState getbranchstate {
		get {
			Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");
			if (!branchStates.ContainsKey(path)) {
				return null;
			} else {
				return branchStates(path);
				// branchStates(bi.path)
			}
		}
	}



	public static void setBranchState(UInt64 lid, string path, clsBranchState bs)
	{
		Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");
		branchStates(path) = bs;
		// branchStates(bi.path)

	}


	public static clsBranchState getBranchStateAbove {

		get {
			if (getbranchstate(lid, "tree") == null) {
				clsBranchState aboveroot = new clsBranchState(lid, "tree", enumBt.OpenSquare, false, 0, 100);
				//the 'All products' renders as a breadcrumb
			}


			if (path == "") {
				errormessages.Add("Path was blank in getBranchStateAbove");
				return null;
			} else {
				if (InStr(path, ".") == 0) {
					errormessages.Add("Path contained no . in getBranchStateAbove");
					return null;
				} else {
					do {
						path = Left(path, InStrRev(path, ".") - 1);
						clsBranchState bs = getbranchstate(lid, path);
						if (bs != null)
							return bs;

						if (InStr(path, ".") == 0) {
							errormessages.Add("No dots left in getBranchStateAbove");
							return null;
						}

					} while (true);

				}

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


	public clsBranchState(ulong lid, path, enumBt rca, bool unite, int rownum, int maxchildren)
	{
		this.rca = rca;
		this.rownum = rownum;
		this.maxChildren = maxchildren;
		this.United = unite;

		Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");

		if (!branchStates.ContainsKey(path)) {
			branchStates.Add(path, this);
		} else {
			branchStates(path) = this;
			//happens when we autoopen a branch already autoopened
		}
		this.timestamp = Now;


	}

	public static object removeBranchState(UInt64 lid, path)
	{

		Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");
		branchStates.Remove(path);

	}


	public static void HideSiblings(UInt64 lid, path)
	{
		Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");

		foreach ( p in branchStates.Keys.ToArray) {
			if (plen(p) == plen(path) & p != path) {
				branchStates(p).rca = enumBt.Hidden;
			}
		}
	}

	public static int plen(path)
	{
		plen = Split(path, ".").Count;

	}

	public static void closeBranchesNotOnPath(UInt64 lid, path)
	{
		//Sets the sesh variables affecting the tree rendering
		//                                                                                                           Path
		Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");

		foreach ( p in branchStates.Keys.ToArray) {
			if (plen(p) > plen(path)) {
				//this is below the path
				branchStates.Remove(p);
			} else {
				if (p != Left(path, Len(p))) {
					branchStates.Remove(p);
				}
			}
		}

	}



	public static void CloseBelow(UInt64 lid, path)
	{
		//during a resize - this gets called by multiple threads .. and it can remove a branchstate twice
		//the synclock may or may not 'solve' the problem - but it would nice to fully understand the root cause

		lock (alock) {
			//Sets the sesh variables affecting the tree rendering
			//                                                                                                           Path
			//Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
			Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates");
			//;, Dictionary(Of String, clsBranchState)

			//branchStates.Clear() TESTING

			foreach ( p in branchStates.Keys.ToArray) {

				if (plen(p) > plen(path)) {
					//branchStates(p).rca = enumBt.Hidden
					branchStates.Remove(p);

				}

				if (plen(p) == plen(path) && branchStates(p).rca == enumBt.OpenSquare)
					branchStates.Remove(p);
			}
		}

	}
	public static void CloseSiblings(UInt64 lid, string path)
	{
		Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates");
		//;, Dictionary(Of String, clsBranchState)

		//branchStates.Clear() TESTING
		string pth = "";
		if (path.Split(".").Length > 1)
			pth = path.Substring(0, path.Length - path.Split(".").Last.Length);

		foreach ( p in branchStates.Keys.ToList().Where(f => f.StartsWith(pth))) {
			//If branchStates(p).rca = enumBt.OpenBranch Then
			if (p != path)
				branchStates(p).rca = enumBt.Hidden;
			// End If
		}
	}

	public static void CloseAbove(UInt64 lid, path)
	{
		//Sets the sesh variables affecting the tree rendering
		//                                                                                                           Path
		//Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
		Dictionary<string, clsBranchState> branchStates = iq.sesh(lid, "branchStates");
		//;, Dictionary(Of String, clsBranchState)

		//branchStates.Clear() TESTING

		foreach ( p in branchStates.Keys.ToArray) {

			if (plen(p) <= plen(path) && (branchStates(p).rca == enumBt.DetailSquare | branchStates(p).rca == enumBt.Square)) {
				branchStates(p).rca = enumBt.OpenSquare;
				// branchStates.Remove(p)

			}
		}

	}

	/// <summary>Returns the subsection of the path down to (and including) the system unit (if present)</summary>
	public static string pathToSystem(path)
	{

		object pth = "";
		foreach ( seg in Split(path, ".")) {
			pth += seg;
			if (seg != "tree") {
				if (iq.Branches((int)seg).Product != null) {
					if (iq.Branches((int)seg).Product.isSystem) {
						break; // TODO: might not be correct. Was : Exit For
					}
				}
			}
			pth += ".";
		}

		if (pth.EndsWith("."))
			pth = Left(pth, Len(pth) - 1);
		return pth;

	}

	public static bool HasSystem(path)
	{

		HasSystem = false;
		foreach ( seg in Split(path, ".")) {
			if (seg != "tree") {
				if (iq.Branches((int)seg).Product != null) {
					if (iq.Branches((int)seg).Product.isSystem) {
						return true;
					}
				}
			}
		}
	}


	//, treewidth As Single)
	public static void PloughPath(UInt64 lid, path, ref List<string> errorMessages, float treewidth, enumParadigm paradigm)
	{

		//Sets the sesh variables to Open all the branches on the path appropriately for the 'show in tree' function

		object pp;
		object pth = "tree";

		string[] segs = Split(path, ".");
		//Dim hitSystem As Boolean = False

		//Dim lastButOne = segs(UBound(segs) - 1)
		foreach ( seg in segs) {

			if (seg != "tree") {
				pp = pth;
				pth += "." + seg;

				//we want to keep state if it's there - and if not use .open to make it
				clsBranchState bs = getbranchstate(lid, pth);
				clsBranchInfo bi = new clsBranchInfo(lid, pth, null, treewidth, paradigm, errorMessages);

				// If bs Is Nothing Then
				bs = bi.open(errorMessages, false);
				//this branch has not yet been opened (rendered)               
				//*KW If bs.rca = enumBt.Square Or bs.rca = enumBt.DetailSquare Then CloseAbove(lid, pth$)
				if ((enumParadigm)iq.sesh(lid, "Paradigm") == enumParadigm.configuringSystem) {
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

