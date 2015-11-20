Imports dataAccess

Public Class clsstock  'Should really be called 'shipments' - many can exist for a single variant (future shipments) 

    'Shipments are (real or predicted) absolute stock positions 
    'ONE shipment is flagged as current

    Property ID As Integer
    'Public Product As clsProduct  'We save memory by *not* having there (at the expense of a more difficult lookup from the Stock Instance)
    ' Property Seller As clsChannel
    Property SKUvariant As clsVariant
    Property quantity As Integer
    Property Arrival As DateTime 'The point in time at which this (total) quantity will be available
    Property LastUpdated As Date  'timestamp of the record
    Property Source As String
    Property IsCurrent As Boolean  'only one stock record per product/variant/seller channel can be current (enforced by a unique index)

    '    Dim oSeller As clsChannel
    '    Dim oProduct As clsProduct
    Dim oSKUVariant As clsVariant
    Dim oArrival As Date


    Public Shared Function temporaryID()

        'assigned the next avialble (and negative) temporary ID for a (in memory only for now) clsPrice
        'INSERTs which would normally yield an ID are very slow - and we don't actually want to persist a record until it has a price (back from the webserivce) anyway

        Dim lock As New Object

        SyncLock lock
            Static countdown As Integer
            countdown -= 1

            temporaryID = countdown
        End SyncLock



    End Function

    Public Sub New(SKUvariant As clsVariant, Quantity As Integer, Arrival As Date, Source As String, iscurrent As Boolean)

        'If Arrival.Date = CDate("01/01/2000").Date Then
        ' Me.IsCurrent = True
        ' End If

        'If Me.IsCurrent Then Arrival = CDate("01/01/2000").Date

        '    Me.Product = Product
        '    Me.Seller = Seller
        Me.SKUvariant = SKUvariant
        Me.quantity = Quantity
        Me.Arrival = Arrival
        Me.LastUpdated = Now
        Me.IsCurrent = iscurrent


        With SKUvariant
            If Not .shipments.ContainsKey(Me.Arrival) Then  'Because some rows are duplictaed in the feeds
                .shipments.Add(Me.Arrival, Me)
            Else
                .shipments(Me.Arrival) = Me
            End If

            'stock is no longer in the products - but is global
            'If Not .Product.i_Stock.ContainsKey(.sellerChannel) Then .Product.i_Stock.Add(.sellerChannel, New Dictionary(Of clsVariant, SortedDictionary(Of Date, clsstock)))
            'If Not .Product.i_Stock(.sellerChannel).ContainsKey(SKUvariant) Then .Product.i_Stock(.sellerChannel)(SKUvariant) = New SortedDictionary(Of Date, clsStock)
            '.Product.i_Stock(.sellerChannel)(SKUvariant).Add(Arrival, Me)
            '  iq.Stock.Add(Me.ID, Me)

            '            oProduct = Product 'keep a record of the 'original' versions - so that we can maintain the index when things are opdated
            '            oSeller = Seller
        End With


        If Quantity = -1 Then
            Me.ID = CInt(temporaryID())
        Else
            Dim sql$
            sql$ = "INSERT INTO STOCK(FK_variant_ID, quantity,Arrival,datestamp,isCurrent,source) VALUES "
            sql$ &= "(" & SKUvariant.ID & "," & Quantity & "," & da.UniversalDate(Arrival) & ",getdate()," & IIf(iscurrent, "1", "0") & "," & da.SqlEncode(Source) & ");"

            Me.ID = da.DBExecutesql(sql$, True)

        End If

        iq.Stock.Add(Me.ID, Me)



        oSKUVariant = SKUvariant
        oArrival = Arrival

    End Sub

    Public Function Insert() As clsstock

        Return New clsstock(Me.SKUvariant, Me.quantity, Me.Arrival, Me.Source, Me.IsCurrent)

    End Function

    Public Sub New(ID As Integer, SKUVariant As clsVariant, Quantity As Integer, Arrival As Date, datestamp As Date, isCurrent As Boolean, ByRef errormessages As List(Of String))

        Me.ID = ID
        'Me.Product = product
        ' Me.Seller = Seller

        Me.SKUvariant = SKUVariant
        Me.quantity = Quantity
        Me.Arrival = Arrival
        Me.LastUpdated = datestamp
        Me.IsCurrent = isCurrent


        If iq.Stock.ContainsKey(Me.ID) Then

            errormessages.Add("* Duplicate stockID & me.id !")
        Else

            iq.Stock.Add(Me.ID, Me)

            With SKUVariant

                If Not .shipments.ContainsKey(Arrival) Then
                    .shipments.Add(Arrival, Me)
                    oSKUVariant = SKUVariant
                    oArrival = Arrival
                Else
                    ' errormessages.Add("* Duplicate shipment date ! SVID:" & SKUVariant.ID & " (" & Arrival.ToString & ") SID:" & Me.ID)

                End If
            End With
        End If

    End Sub

    Public Function update() As clsstock

        Dim sql$
        sql$ = "UPDATE [Stock] SET quantity =" & Me.quantity & ",arrival=" & da.UniversalDate(Me.Arrival) & ",dateStamp=" & da.UniversalDate(Me.LastUpdated) & ",isCurrent=" & IIf(Me.IsCurrent, "1", "0")
        sql$ &= " WHERE ID = " & Me.ID
        da.DBExecutesql(sql$)

        oSKUVariant.shipments.Remove(oArrival)
        SKUvariant.shipments(Arrival) = Me

        Return Me

    End Function

    Public Sub Delete()

        Dim sql$
        sql$ = "DELETE FROM [Stock] WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)

        oSKUVariant.shipments.Remove(oArrival)

    End Sub



End Class
