

public class clsProfile
{


	//for each labelled profile (eg. 'SaveQuote') - we store an array of stats for every recursion level - higher levels (lower numbers) include calls to deeper ones
	private struct stcStats
	{

		public Int64 TotalTicks;
		public double Mark;
		public Int64 Calls;
		public Int64 Min;

		public Int64 Max;
	}

	//Carries a set of stats for each level of recursion - ie.. wh

	private stcStats[] stats;
	private int depth;

	private int maxDepth;

	public void PMark()
	{
		this.depth = this.depth + 1;
		if (this.depth > this.maxDepth)
			this.maxDepth = this.depth;
		//how 'deep' did we go (recusion depth)
		if (this.depth > 40)
			System.Diagnostics.Debugger.Break();
		if (this.depth > UBound(this.stats))
			Array.Resize(ref this.stats, UBound(this.stats) + 6);
		this.stats(this.depth).Mark = System.Diagnostics.Stopwatch.GetTimestamp;

	}


	public void PAccumulate()
	{
		Int64 et;
		//elapsed time (in stopwatch ticks)
		 // ERROR: Not supported in C#: WithStatement

		this.depth = this.depth - 1;

		//If Me.depth < 0 Then Stop

	}

	public List<TableRow> Results(name)
	{

		//returna list of tabel rows - one for each recusion depth level (for the sub/fuction being profiled)

		Results = new List<TableRow>();
		TableRow tr;
		TableCell td;
		for (i = 1; i <= this.maxDepth; i++) {
			tr = new TableRow();
			Results.Add(tr);


				//Procedure lable and recursion depth






			 // ERROR: Not supported in C#: WithStatement

		}

	}

	public clsProfile()
	{
		 // ERROR: Not supported in C#: ReDimStatement

	}

}
