Imports dataAccess

Public Class clsMargin

    'A margin is an amount of money made on the sale of each product
    'It is expressed as a factor - by which the base price for each product is multiplied 
    '(each seller has a base price for each product.. see Product.prices(seller))

    'A factor of 1.05 is 'cost plus 5%'
    'retained margins can also be expressed as a simple factor, 5% retained margin is a factor of 1.052631578947368
    'each (selling) channel - has a dictionary of Margins for each of its buying customers

    'Retained/CostPlus margin calculations

    'SELECT @value=CONVERT(Decimal(12,2),CONVERT(Money, CASE @mode '
    'WHEN 'ret' THEN 1/(1-(@margin*0.01))*@value 
    'WHEN 'cplus' THEN @value*(100+@margin)*0.01 END))
    'Return @value
    'END

    Property ID As Integer
    Public Seller As clsChannel  'these are members, not properties - becuase we don't want them exposed for editing
    Public buyer As clsChannel
    Property Factor As Single
    'Property ProductType As clsProductType - We should replace Sector with a Hierarchy of product types Where the current sectors are the l1 Brances (ISS/PSG) - L2 might be system types - with a sepeate set of optios
    Property SampledSKU As String
    Property Sector As clsSector
    Property PriceBand As String 'which price is used as the source for prices generated via this margin
    Property bad As Boolean = False 'used during import - inconsistent margins ar removed and replaced with per buyer/seller/vairant prices

    Public Sub New(id As Integer, seller As clsChannel, buyer As clsChannel, factor As Single, priceband As String, sector As clsSector, sampledSKU As String)

        Me.ID = id
        Me.Seller = seller
        Me.buyer = buyer
        Me.Factor = factor
        '   Me.ProductType = producttype
        Me.SampledSKU = sampledSKU
        Me.Sector = sector
        Me.PriceBand = priceband

        'add the margin for this ProductType to the correct seller/buyer
        If Not seller.Margin.ContainsKey(buyer) Then
            seller.Margin.Add(buyer, New Dictionary(Of clsSector, clsMargin))
        End If

        'If Not seller.Margin(buyer).ContainsKey(sector) Then
        ' seller.Margin(buyer).Add(sector, New Dictionary(Of clsProductType, clsMargin))
        ' End If


    End Sub

    Public Sub New(seller As clsChannel, buyer As clsChannel, factor As Single, Priceband As String, sector As clsSector, sampledsku As String)

        Dim sql$
        sql$ = "INSERT INTO Margin (fk_Channel_id_seller,fk_channel_id_buyer,factor,priceband,sampledsku,fk_sector_id) "
        sql$ &= " VALUES (" & seller.ID & "," & buyer.ID & "," & factor & "," & da.SqlEncode(Priceband) & "," & da.SqlEncode(sampledsku) & "," & sector.ID & ");"

        Me.ID = da.DBExecutesql(sql$, True)

        'call the 'other' constructor - to get it added to the seller.margin(buyer) dictionary
        Dim aMargin As clsMargin
        aMargin = New clsMargin(Me.ID, seller, buyer, factor, Priceband, sector, sampledsku)

        Me.Sector = sector

    End Sub


End Class
