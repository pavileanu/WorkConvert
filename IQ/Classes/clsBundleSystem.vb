Imports dataAccess

Public Class clsBundleSystem

    Property ID As Integer
    Property Bundle As clsBundle
    Property System As clsProduct
    Property rebate As Single

    Public Sub New(bundle As clsBundle, system As clsProduct, rebate As Single, Optional writecache As DataTable = Nothing)

        'price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
        'Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers

        If writecache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO BundleSystem(fk_Bundle_id,fk_product_id_system,rebate) VALUES (" & bundle.ID & "," & bundle.ID & "," & system.ID & "," & rebate & ")"
            Me.ID = da.DBExecutesql(sql$)
            If system.Bundles Is Nothing Then system.Bundles = New Dictionary(Of Integer, clsBundle)
            system.Bundles.Add(bundle.ID, bundle)

        Else
            Me.ID = -1

            Dim row As System.Data.DataRow
            row = writecache.NewRow()

            row("fk_bundle_id") = bundle.ID
            row("fk_product_id_system") = system.ID
            row("rebate") = rebate
            writecache.Rows.Add(row)

        End If

        Me.Bundle = bundle
        Me.System = system
        Me.rebate = rebate

    End Sub

    Public Sub New(ID As Integer, bundle As clsBundle, system As clsProduct, rebate As Single)

        Me.ID = ID
        Me.Bundle = bundle
        Me.System = system
        Me.rebate = rebate

        If system.Bundles Is Nothing Then system.Bundles = New Dictionary(Of Integer, clsBundle)
        system.Bundles.Add(bundle.ID, bundle)


    End Sub


End Class
