using Microsoft.VisualBasic.CompilerServices;



public class clsProfile
{


    //for each labelled profile (eg. 'SaveQuote') - we store an array of stats for every recursion level - higher levels (lower numbers) include calls to deeper ones
    private struct stcStats
    {

        public long TotalTicks;
        public double Mark;
        public long Calls;
        public long Min;
        public long Max;

    }

    //Carries a set of stats for each level of recursion - ie.. wh

    private stcStats[] stats;
    private int depth;
    private int maxDepth;

    public void PMark()
    {

        this.depth++;
        if (this.depth > this.maxDepth)
        {
            this.maxDepth = this.depth; //how 'deep' did we go (recusion depth)
        }
        if (this.depth > 40)
        {
            Debugger.Break();
        }
        if (this.depth > (this.stats.Length - 1))
        {
            Array.Resize(ref this.stats, (this.stats.Length - 1) + 5 + 1);
        }
        this.stats[this.depth].Mark = System.Convert.ToDouble(System.Diagnostics.Stopwatch.GetTimestamp);

    }

    public void PAccumulate()
    {

        long et = 0; //elapsed time (in stopwatch ticks)
        stcStats with_1 = this.stats[this.depth];
        et = System.Convert.ToInt64(System.Diagnostics.Stopwatch.GetTimestamp - with_1.Mark);
        with_1.TotalTicks += et;
        with_1.Calls += 1;
        if (et < with_1.Min || with_1.Min == 0)
        {
            with_1.Min = et;
        }
        if (et > with_1.Max)
        {
            with_1.Max = et;
        }
        this.depth--;

        //If Me.depth < 0 Then Stop

    }

    public List<TableRow> Results(object name)
    {
        List<TableRow> returnValue = default(List<TableRow>);

        //returna list of tabel rows - one for each recusion depth level (for the sub/fuction being profiled)

        returnValue = new List<TableRow>();
        TableRow tr = default(TableRow);
        TableCell td = default(TableCell);
        for (i = 1; i <= this.maxDepth; i++)
        {
            tr = new TableRow();
            returnValue.Add(tr);

            stcStats with_1 = stats[i];
            int totalMs = 0;
            totalMs = System.Convert.ToInt32(System.Convert.ToInt32(with_1.TotalTicks / Stopwatch.Frequency) * 1000);

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = name + " depth" + i; //Procedure lable and recursion depth

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = Strings.Format(with_1.Calls, "##");

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = Strings.Format(totalMs, "##");

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = Strings.Format(totalMs / with_1.Calls * 1000, "##");

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = Strings.Format(System.Convert.ToInt32(with_1.Min / Stopwatch.Frequency) * 1000000, "##");

            td = new TableCell();
            tr.Controls.Add(td);
            td.Text = Strings.Format(System.Convert.ToInt32(with_1.Max / Stopwatch.Frequency) * 1000, "##");

        }

        return returnValue;
    }

    public clsProfile()
    {
        this.stats = new stcStats[6];
    }

}