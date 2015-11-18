Imports dataAccess

Public Class clsPool

    Property id As Integer
    Public products As Dictionary(Of Integer, clsProduct)

    Public Sub New()

        Me.products = New Dictionary(Of Integer, clsProduct)


    End Sub

    Public Sub New(id As Integer)

        Me.id = id
        Me.products = New Dictionary(Of Integer, clsProduct)

    End Sub

    Public Function insert() As clsPool

        Dim apool As clsPool = New clsPool(Me.id)

        'iq.pools.add(apool.id, apool)

        Return apool

    End Function

End Class


Public Class clsOffer

    'an offer changes the price of a product.. one offer can
    '20% of HDD's in a certain machine (if you buy 4)

    Property ID As Integer
    Property name As clsTranslation                  'FK offer_id
    Property Price As clsPrice
    Property qtyMin As Integer
    Property qtyMax As Integer
    Property absoluteDiscount As Single
    Property percentDiscount As Single
    Property DiscountFromPrice As clsPrice
    Property Pool As clsPool                                       'which pool of products does this offer relate to
    Property poolQtyRequired As Integer                 'how many 
    Property poolDistinctRequired As Integer
    Property MustHaveOneFrom As clsPool
    Property ValidFrom As DateTime
    Property ValidTo As DateTime


    ReadOnly Property DisplayName(language As clsLanguage) As String
        Get
            DisplayName = name.text(language)
        End Get
    End Property
    Public Function Insert() As clsOffer

        Dim anOffer As clsOffer = New clsOffer(Me.name, Me.Price, Me.qtyMin, Me.qtyMin, Me.absoluteDiscount, Me.percentDiscount, Me.DiscountFromPrice, Me.Pool, Me.poolQtyRequired, Me.poolDistinctRequired, Me.MustHaveOneFrom, Me.ValidFrom, Me.ValidTo)

        Me.Price.Offers.Add(Me.ID, Me)

        Return anOffer

    End Function

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE OFFER SET "
        sql$ &= "name=" & Me.name.Key & ","
        sql$ &= "FK_Price_ID=" & Me.Price.ID & ","
        sql$ &= "qtymin=" & Me.qtyMin & ","
        sql$ &= "qtymax=" & Me.qtyMax & ","
        sql$ &= "absoluteDiscount=" & Me.absoluteDiscount & ","
        sql$ &= "percentDiscount =" & Me.percentDiscount & ","
        sql$ &= "fk_price_id_discountFrom=" & Me.DiscountFromPrice.ID & ","
        sql$ &= "fk_pool_id" & Me.Pool.id & ","
        sql$ &= "PoolQtyRequired=" & Me.poolQtyRequired & ","
        sql$ &= "PoolDistinctRequired=" & Me.poolDistinctRequired & ","
        sql$ &= "fk_pool_id_mustahaveone= " & Me.MustHaveOneFrom.id & ","
        sql$ &= "validfrom= " & da.universaldate(Me.ValidFrom) & ","
        sql$ &= "validto =  " & da.universaldate(Me.ValidTo)

        sql$ &= " WHERE ID = " & Me.ID

        da.DBExecutesql(sql)

    End Sub

    Public Sub New(Name As clsTranslation, Price As clsPrice, qtyMin As Integer, qtyMax As Integer, absoluteDiscount As Single, percentDiscount As Integer, DiscountFromPrice As clsPrice, Pool As clsPool, poolQtyRequired As Integer, poolDistinctRequired As Integer, MustHaveOneFrom As clsPool, validFrom As DateTime, validTo As DateTime, Optional writecache As DataTable = Nothing)

        Me.name = Name
        Me.Price = Price
        Me.qtyMin = qtyMin
        Me.qtyMax = qtyMax
        Me.absoluteDiscount = absoluteDiscount
        Me.percentDiscount = percentDiscount
        Me.DiscountFromPrice = DiscountFromPrice
        Me.Pool = Pool
        Me.poolQtyRequired = poolQtyRequired
        Me.poolDistinctRequired = poolDistinctRequired
        Me.MustHaveOneFrom = MustHaveOneFrom

        If writecache Is Nothing Then

            Dim sql$
            sql$ = "INSERT INTO Offer (fk_translation_key_name,fk_price_id,qtyMin,qtyMax,absoluteDiscount,percentDiscount,fk_price_id_discountFrom,fk_Pool_id,poolQtyRequired,PoolDistinctRequired,fk_pool_id_mustHaveOne,validFrom,validTo) "
            sql$ &= "VALUES (" & Name.Key & "," & Price.ID & "," & qtyMin & "," & qtyMax & "," & absoluteDiscount & "," & percentDiscount & "," & DiscountFromPrice.ID & "," & Pool.id & "," & poolQtyRequired & "," & MustHaveOneFrom.id & "," & da.universaldate(validFrom) & "," & da.universaldate(validTo) & ")"

            Me.ID = da.DBExecutesql(sql$, True)
        Else

            Dim row As System.Data.DataRow
            row = writecache.NewRow()

            row("fk_translation_key_name") = Me.name.Key
            row("fk_price_id") = Me.Price.ID
            row("qtymin") = Me.qtyMin
            row("qtymax") = Me.qtyMax
            row("absoluteDiscount") = Me.absoluteDiscount
            row("percentDiscount") = Me.percentDiscount
            row("fk_price_id_discountfrom") = Me.DiscountFromPrice.ID
            row("fk_pool_id") = Me.Pool.id
            row("poolQtyRequired") = Me.poolQtyRequired
            row("poolDistinctRequired") = Me.poolDistinctRequired
            row("FK_pool_id_mustHaveOne") = Me.MustHaveOneFrom   'You must have one product from this pool
            row("validFrom") = Me.ValidFrom
            row("validTo") = Me.ValidFrom

            writecache.Rows.Add(row)

        End If

        Price.Offers.Add(Me.ID, Me)

        'If a system has a descendant price with  offer which points to a pool contaiining the system



    End Sub
    Public Sub New()

        Me.ID = -1

    End Sub


    Public Sub New(ByVal ID As Integer, Name As clsTranslation, Price As clsPrice, qtyMin As Integer, qtyMax As Integer, absoluteDiscount As Single, percentDiscount As Integer, DiscountFromPrice As clsPrice, Pool As clsPool, poolQtyRequired As Integer, poolDistinctRequired As Integer, MustHaveOneFrom As clsPool, validfrom As DateTime, validto As DateTime, Optional writecache As DataTable = Nothing)

        Me.ID = ID

        Me.name = Name
        Me.Price = Price
        Me.qtyMin = qtyMin
        Me.qtyMax = qtyMax
        Me.absoluteDiscount = absoluteDiscount
        Me.percentDiscount = percentDiscount
        Me.DiscountFromPrice = DiscountFromPrice
        Me.Pool = Pool
        Me.poolQtyRequired = poolQtyRequired
        Me.poolDistinctRequired = poolDistinctRequired
        Me.MustHaveOneFrom = MustHaveOneFrom
        Me.ValidFrom = validfrom
        Me.ValidTo = validto

        Me.Price.Offers.Add(Me.ID, Me)

    End Sub

End Class
