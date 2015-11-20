
public class LoggingList<T>
{
	//Inherits List(Of String)
	private class clsData
	{
		public string Message;
		public double HeapSize;
		public double Time;
	}
	private List<clsData> data = new List<clsData>();
	private Stopwatch sw = new Stopwatch();

	private double StartBytes;
	public void Clear()
	{
		sw.Reset();
		data.Clear();
	}
	public void Start()
	{
		sw.Start();
		StartBytes = System.GC.GetTotalMemory(true);
	}
	public void StopClock()
	{
		sw.stop();
	}
	public object ToList()
	{
		return data.ToList();
	}
	public void Add(string o)
	{
		data.Add(new clsData {
			HeapSize = Math.Round((System.GC.GetTotalMemory(false) - StartBytes) / (Math.Pow(1024, 2)), 2),
			Time = sw.ElapsedMilliseconds / 1000,
			Message = o
		});
	}
}
