Imports dataAccess
Public Class clsBundleItem

    Property ID As Integer
    Property Bundle As clsBundle
    Property qtyMin As Integer
    Property Price As nullablePrice 'encapsulates currency
    Property Rebate As Single
    Property Product As clsProduct

    Public Sub New(bundle As clsBundle, product As clsProduct, price As nullablePrice, rebate As Single, qytMin As Integer, Optional WriteCache As DataTable = Nothing)

        'price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
        'Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers



        If WriteCache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO BundleItem(fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtymin) VALUES (" & bundle.ID & "," & product.ID & "," & price.sqlvalue & "," & rebate & "," & price.currency.ID & "," & qtyMin & ")"
            Me.ID = da.DBExecutesql(sql$)
            bundle.Items.Add(Me.ID, Me)
        Else
            Me.ID = -1

            Dim row As System.Data.DataRow
            row = WriteCache.NewRow()

            row("fk_bundle_id") = bundle.ID
            row("fk_product_id") = product.ID
            row("price") = IIf(price.sqlvalue = "null", DBNull.Value, price.NumericValue)
            row("rebate") = rebate
            row("fk_currency_id") = price.currency.ID
            row("qtymin") = qtyMin

            WriteCache.Rows.Add(row)

        End If

        Me.Bundle = bundle
        Me.Product = product
        Me.Price = price
        Me.qtyMin = qtyMin


    End Sub

    Public Sub New(ID As Integer, bundle As clsBundle, product As clsProduct, price As nullablePrice, rebate As Single, qytMin As Integer)

        Me.ID = ID
        bundle.Items.Add(Me.ID, Me)
        Me.Bundle = bundle
        Me.Product = product
        Me.Price = price
        Me.Rebate = rebate
        Me.qtyMin = qtyMin


    End Sub


End Class
