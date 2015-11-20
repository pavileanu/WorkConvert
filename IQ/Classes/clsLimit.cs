public class clsLimit
{

    //[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref]
    public int Qinstalled { get; set; }
    public int Qmin { get; set; }
    public int Qmax { get; set; }
    public int MinIncr { get; set; }
    public int PrefIncr { get; set; }

    public clsLimit(int Installed, int Qmin, int Qmax, int MinIncr, int PrefIncr)
    {

        this.Qinstalled = Installed;
        this.Qmin = Qmin;
        this.Qmax = Qmax;
        this.MinIncr = MinIncr;
        this.PrefIncr = PrefIncr;

    }

}