public class LoggingList<T>
{
    public LoggingList()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        data = new List<clsData>();
        sw = new Stopwatch();

    }
    //Inherits List(Of String)
    private class clsData
    {
        public string Message;
        public double HeapSize;
        public double Time;
    }
    private List<clsData> data; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    private Stopwatch sw; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    private double StartBytes;

    public void Clear()
    {
        sw.Reset();
        data.Clear();
    }
    public void Start()
    {
        sw.Start();
        StartBytes = System.Convert.ToDouble(System.GC.GetTotalMemory(true));
    }
    public void StopClock()
    {
        sw.stop();
    }
    public dynamic ToList()
    {
        return data.ToList();
    }
    public void Add(string o)
    {
        data.Add(new clsData() { HeapSize = Math.Round((System.GC.GetTotalMemory(false) - StartBytes) / (Math.Pow(1024, 2)), 2), Time = sw.ElapsedMilliseconds / 1000, Message = o });
    }
}