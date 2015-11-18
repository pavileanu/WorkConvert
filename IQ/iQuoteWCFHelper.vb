Module iQuoteWCFHelper


    Public Class clsStockLine

        'This class in returned by clsStockBatch.add line - and is used to hold a reference to each underlying StockServiceStockItem - so that it can expose an AddShipMent method

        Private line As WCFSvc.WCFsvc_clsStockItem

        Public Sub AddShipment(quantity As Integer, Arrival As Date)

            'add a shipment with an arrival date of 01/01/2000 to indicate current stock

            ReDim Preserve line.shipments(UBound(line.shipments) + 1)
            Dim shipment As New WCFSvc.WCFsvc_clsstock
            shipment.Quantity = quantity
            shipment.Arrival = Arrival
            line.shipments(UBound(line.shipments)) = shipment

        End Sub

        Public Sub New(ln As WCFSvc.WCFsvc_clsStockItem)
            Me.line = ln
        End Sub

    End Class

    Public Class clsStockBatch

        Public lines As List(Of WCFSvc.WCFsvc_clsStockItem)

        'This local class is used to Hold and maintain a set of instances of the remote WCF Class, Prior to submitting them as a single transaction

        Public Sub New()
            lines = New List(Of WCFSvc.WCFsvc_clsStockItem)
        End Sub

        Public Function addLine(sku As String, SKUvariant As String, Quantity As String, Arrival As Date) As clsStockLine

            Dim si As New WCFSvc.WCFsvc_clsStockItem
            si.SKU = sku
            si.SKUvariant = SKUvariant
            ReDim si.shipments(0)
            si.shipments(0) = New WCFSvc.WCFsvc_clsstock
            si.shipments(0).Arrival = arrival
            si.shipments(0).Quantity = Quantity


            lines.Add(si)

            addLine = New clsStockLine(si) 'return a reference to an object holding a reference to the stock item, and exposing an addShipment method

        End Function

    End Class

    Public Class clsPriceBatch

        Public lines As List(Of WCFSvc.WCFsvc_clsPrice)

        Public Sub New()
            lines = New List(Of WCFSvc.WCFsvc_clsPrice)
        End Sub

        Public Sub AddLine(SKU As String, SKUvariant As String, GroupID As String, Price As Decimal, Currency As String)

            Dim p As WCFSvc.WCFsvc_clsPrice
            p = New WCFSvc.WCFsvc_clsPrice
            With p
                .SKU = SKU
                .SKUvariant = SKUvariant
                .GroupID = GroupID
                .Price = Price
                .Currency = Currency
            End With

            lines.Add(p)
        End Sub

    End Class


End Module
