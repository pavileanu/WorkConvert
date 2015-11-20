class iQuoteWCFHelper
{


	public class clsStockLine
	{

		//This class in returned by clsStockBatch.add line - and is used to hold a reference to each underlying StockServiceStockItem - so that it can expose an AddShipMent method


		private WCFSvc.WCFsvc_clsStockItem line;

		public void AddShipment(int quantity, System.DateTime Arrival)
		{
			//add a shipment with an arrival date of 01/01/2000 to indicate current stock

			Array.Resize(ref line.shipments, UBound(line.shipments) + 2);
			WCFSvc.WCFsvc_clsstock shipment = new WCFSvc.WCFsvc_clsstock();
			shipment.Quantity = quantity;
			shipment.Arrival = Arrival;
			line.shipments(UBound(line.shipments)) = shipment;

		}

		public clsStockLine(WCFSvc.WCFsvc_clsStockItem ln)
		{
			this.line = ln;
		}

	}

	public class clsStockBatch
	{


		public List<WCFSvc.WCFsvc_clsStockItem> lines;
		//This local class is used to Hold and maintain a set of instances of the remote WCF Class, Prior to submitting them as a single transaction

		public clsStockBatch()
		{
			lines = new List<WCFSvc.WCFsvc_clsStockItem>();
		}

		public clsStockLine addLine(string sku, string SKUvariant, string Quantity, System.DateTime Arrival__1)
		{

			WCFSvc.WCFsvc_clsStockItem si = new WCFSvc.WCFsvc_clsStockItem();
			si.SKU = sku;
			si.SKUvariant = SKUvariant;
			 // ERROR: Not supported in C#: ReDimStatement

			si.shipments(0) = new WCFSvc.WCFsvc_clsstock();
			si.shipments(0).Arrival = arrival;
			si.shipments(0).Quantity = Quantity;


			lines.Add(si);

			addLine = new clsStockLine(si);
			//return a reference to an object holding a reference to the stock item, and exposing an addShipment method

		}

	}

	public class clsPriceBatch
	{


		public List<WCFSvc.WCFsvc_clsPrice> lines;
		public clsPriceBatch()
		{
			lines = new List<WCFSvc.WCFsvc_clsPrice>();
		}


		public void AddLine(string SKU, string SKUvariant, string GroupID, decimal Price, string Currency)
		{
			WCFSvc.WCFsvc_clsPrice p;
			p = new WCFSvc.WCFsvc_clsPrice();
			 // ERROR: Not supported in C#: WithStatement


			lines.Add(p);
		}

	}


}
