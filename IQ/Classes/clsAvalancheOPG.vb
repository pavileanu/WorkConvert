Imports dataAccess

Public Class ClsAvalancheOPG

    'systems carry a list of avalancheOPG's
    Property ID As Integer
    Property OPGref
    Property Region As clsRegion
    Property ValidFrom As DateTime
    Property ValidTo As DateTime
    Property OptMin As Integer
    Property OptMax As Integer
    'Property LPDiscountPercent As Single 'percent discount off list price (prices(HP))
    Property Options As Dictionary(Of Integer, ClsAvalancheOption)

    ReadOnly Property DisplayName(language As clsLanguage) As String
        Get
            DisplayName = OPGref
        End Get
    End Property
    Public Function Insert() As ClsAvalancheOPG

        Dim av As ClsAvalancheOPG = New ClsAvalancheOPG(Me.OPGref, Me.region, Me.ValidFrom, Me.ValidTo, Me.OptMin, Me.OptMax)
        Return av

    End Function

    Public Function getAvalancheOptions(Optional prodref As String = "", Optional qty As Integer = 0, Optional dateTime As Object = Nothing, Optional region As clsRegion = Nothing) As List(Of clsAvalancheOption)

        Pmark("getAvalancheOptions")
        'returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)

        getAvalancheOptions = New List(Of clsAvalancheOption)

        Dim dateValid As Boolean = False
        If dateTime Is Nothing Then
            dateValid = True
        Else
            If dateTime > Me.ValidFrom And dateTime < Me.ValidTo Then
                dateValid = True
            End If
        End If

        Dim regionValid As Boolean = False
        If region Is Nothing Then
            regionValid = True
        Else
            regionValid = Me.Region.Encompasses(region)
        End If

        If dateValid Then
            If (qty >= Me.OptMin And qty <= Me.OptMax) Or qty = 0 Then
                If regionValid Then
                    For Each o In Me.Options.Values
                        If o.ProdRef = prodref Or prodref = "" Then
                            'avalanche gives a discount as a percentage of list price
                            getAvalancheOptions.Add(o)
                        End If
                    Next
                End If
            End If
        End If

        Pacc("getAvalancheOptions")

    End Function

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE Avalance SET "
        sql$ &= "opgRef=" & Me.OPGref & ","
        '  sql$ &= "prodRef=" & Me.ProdRef & ","
        sql$ &= "FK_region_id=" & Me.region.ID & ","
        sql$ &= "validFrom=" & da.universaldate(Me.ValidFrom) & ","
        sql$ &= "validTo=" & da.universaldate(Me.ValidTo) & ","
        ' sql$ &= "LPDiscountPercent =" & Me.LPDiscountPercent & ","
        sql$ &= "optMin=" & Me.OptMin & ","
        sql$ &= "optMax=" & Me.OptMax & ""
        sql$ &= " WHERE ID = " & Me.ID

        da.DBExecutesql(sql)

        'TODO : - need to update iq.i_OPGref 

    End Sub

    Public Sub New(opgRef As String, Region As clsRegion, validFrom As DateTime, ValidTo As DateTime, optMin As Integer, optMax As Integer, Optional writecache As DataTable = Nothing)

        Me.OPGref = opgRef
        'Me.ProdRef = prodRef
        Me.region = Region
        Me.ValidFrom = validFrom
        Me.ValidTo = ValidTo
        Me.OptMin = optMin
        Me.OptMax = optMax
        Me.Options = New Dictionary(Of Integer, ClsAvalancheOption)

        If writecache Is Nothing Then

            Dim sql$
            sql$ = "INSERT INTO avalancheOPG (opgref,optmin,optmax,validFrom,validTo,fk_region_id) "
            sql$ &= "VALUES (" & opgRef & "," & optMin & "," & optMax & "," & da.universaldate(validFrom) & "," & da.universaldate(ValidTo) & "," & Region.ID & ")"

            Me.ID = da.DBExecutesql(sql$, True)
            iq.AvalancheOPGs.Add(Me.ID, Me)

        Else

            Dim row As System.Data.DataRow
            row = writecache.NewRow()

            row("opgref") = Me.OPGref
            'row("prodref") = Me.ProdRef
            row("optmin") = Me.OptMin
            row("optmax") = Me.OptMax
            'row("LPDiscountPercent") = Me.LPDiscountPercent
            row("validFrom") = Me.ValidFrom
            row("validTo") = Me.ValidTo
            row("fk_region_id") = Me.region.ID

            writecache.Rows.Add(row)

        End If

        iq.i_OpgRef.Add(opgRef, Me)

        'If a system has a descendant price with  offer which points to a pool contaiining the system

    End Sub
    Public Sub New()

        Me.ID = -1


    End Sub


    Public Sub New(ByVal ID As Integer, opgRef As String, Region As clsRegion, validFrom As DateTime, ValidTo As DateTime, optMin As Integer, optMax As Integer)

        'the OPG's don't carry a set of systems - becuase the systems have the OPG's added (product.avalancheOPGs)... which is how they're needed 
        '(we typically want to know the OPG's for a system - not the systems for an OPG)

        Me.ID = ID
        Me.OPGref = opgRef
        Me.region = Region
        Me.ValidFrom = validFrom
        Me.ValidTo = ValidTo
        Me.OptMin = optMin
        Me.OptMax = optMax

        iq.AvalancheOPGs.Add(Me.ID, Me)
        iq.i_OpgRef.Add(opgRef, Me)

        Me.Options = New Dictionary(Of Integer, ClsAvalancheOption)

    End Sub


End Class
