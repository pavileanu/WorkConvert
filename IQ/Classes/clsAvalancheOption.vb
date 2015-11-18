Imports dataAccess
Public Class clsAvalancheOption

    Property ID As Integer
    Property ProdRef As String
    Property LPDiscountPercent As Single
    Property AvalancheOPG As ClsAvalancheOPG

    Public Sub New(avOPG As ClsAvalancheOPG, prodRef As String, LPDiscountPercent As Single, Optional WriteCache As DataTable = Nothing)


        If LPDiscountPercent = 0 Then Stop

        If WriteCache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO AvalancheOption(fk_avalancheOPG_ID,prodref,lpdiscountpercent) VALUES (" & avOPG.ID & "," & da.SqlEncode(prodRef) & "," & LPDiscountPercent & ")"
            Me.ID = da.DBExecutesql(sql$)
            avOPG.Options.Add(Me.ID, Me)
        Else
            Me.ID = -1

            Dim row As System.Data.DataRow
            row = WriteCache.NewRow()

            row("fk_avalancheOPG_id") = avOPG.ID
            row("prodref") = prodRef
            row("LPDiscountPercent") = LPDiscountPercent
            WriteCache.Rows.Add(row)

        End If

        Me.ProdRef = prodRef
        Me.LPDiscountPercent = LPDiscountPercent
        Me.AvalancheOPG = avOPG


    End Sub

    Public Sub New(ID As Integer, avOPG As ClsAvalancheOPG, prodRef As String, LPDiscountPercent As Single)

        Me.ID = ID
        Me.ProdRef = prodRef
        Me.LPDiscountPercent = LPDiscountPercent

        If Me.LPDiscountPercent = 0 Then Stop

        Me.AvalancheOPG = avOPG
        avOPG.Options.Add(Me.ID, Me)

    End Sub

End Class
