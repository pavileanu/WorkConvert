

Public Class clsShoppingListItem

    Property QtyPartNo As String
    Property PartNo As String
    Property Quantity As Integer
    Property Product As clsProduct

    Public Sub New(qtyPartNo As String, partNo As String, quantity As Integer, product As clsProduct)

        Me.QtyPartNo = qtyPartNo
        Me.PartNo = partNo
        Me.Quantity = quantity
        Me.Product = product

    End Sub

End Class
