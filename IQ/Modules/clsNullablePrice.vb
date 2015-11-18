Public Class NullablePrice

    Public value As Decimal 'Object
    Public ReadOnly currency As clsCurrency
    Public isValid As Boolean 'Confirmed Price = false this price is poa   ... this should probably become a datestamp/source combo
    Public isList As Boolean  'Price is - or contains a HP list price element
    Public isTotal As Boolean 'used to alter tooltips to say this price *Includes* HP LIst price elements

    'Public SKUVariant As clsVariant
    Public Message As String

    Public Shared Operator *(P As NullablePrice, margin As Single) As NullablePrice

        Dim newprice As NullablePrice = New NullablePrice(P.value * margin, P.currency, P.isList)
        newprice.isValid = P.isValid  '! very important - otherwise Invalid prices become valid 0 prices when multiplied by a margin !
        newprice.isList = P.isList
        Return newprice

    End Operator

    Public Shared Operator +(p1 As NullablePrice, p2 As NullablePrice) As NullablePrice

        Dim newprice As NullablePrice = New NullablePrice(p1.value + p2.value, p1.currency, p1.isList Or p2.isList)

        If (Not p1.isValid) Or (Not p2.isValid) Then newprice.isValid = False
        newprice.isTotal = True

        Return newprice

    End Operator


    Public Shared Operator -(p1 As NullablePrice, p2 As NullablePrice) As NullablePrice

        Dim newprice As NullablePrice = New NullablePrice(p1.value - p2.value, p1.currency, p1.isList Or p2.isList)

        If (Not p1.isValid) Or (Not p2.isValid) Then newprice.isValid = False
        newprice.isTotal = True

        Return newprice


    End Operator



    Public Sub New(currency As clsCurrency)
        Me.value = 0 '-9999999.4
        Me.isValid = False
        Me.isList = False
        Me.currency = currency

    End Sub


    Public Sub New(value As Object, currency As clsCurrency, isList As Boolean) ', SKUvariant As clsVariant)

        If value Is Nothing Then
            Beep() ' please use the paramaterless constructor e.g. New NullableInt() - to create a 'POA' (invalid) price
        ElseIf value Is DBNull.Value Then
            Beep() ' please use the paramaterless constructor e.g. New NullableInt() - to create a 'POA' (invalid) price
        End If

        Me.currency = currency

        If TypeOf (value) Is Decimal Or TypeOf (value) Is Double Or TypeOf (value) Is Single Then 'TypeOf (value) Is Single Or TypeOf (value) Is Double Or TypeOf (value) Is Decimal Then

            Me.value = CDec(value)
            Me.isValid = True
            Me.isList = isList
        Else
            Beep()
        End If

    End Sub


    'Pair of small helper functions to make the main code more readable elsewhere
    Public Function isDifferentFrom(p As nullablePrice)

        Return Not isSameAs(p)

    End Function

    Public Function isSameAs(p As NullablePrice) As Boolean  'We *could* have overloaded the equals operator - but i think that's potentially confusing

        If p.isValid <> Not Me.isValid Then Return False

        If Me.value = p.value Then Return True Else Return False

    End Function

    Function DisplayPrice(buyerAccount As clsAccount, ByRef errorMessages As List(Of String)) As Panel 'Label

        DisplayPrice = New Panel 'Label - labels WOULD NOT accept a cssclass !

        If Not Me.isValid And Not Me.isList Then  'Prices containing listprice elements can be (are often 'invalid')
            ' If Me.isList Then Stop
            DisplayPrice.Controls.Add(NewLit(Xlt(" ...", buyerAccount.Language)))
            DisplayPrice.CssClass = "POA"
            DisplayPrice.ToolTip = Me.Message
        Else
            DisplayPrice.Controls.Add(NewLit(Me.text(buyerAccount, errorMessages)))

            If Me.isList Then
                If Me.isTotal Then
                    DisplayPrice.ToolTip = Xlt("contains HP List Price element(s)", buyerAccount.Language)
                Else
                    DisplayPrice.ToolTip = Xlt("HP List Price", buyerAccount.Language)
                End If

                DisplayPrice.CssClass &= " listPrice"
            Else
                DisplayPrice.ToolTip = Xlt("Confirmed Price", buyerAccount.Language)

            End If

        End If

    End Function


    Public Function text(buyeraccount As clsAccount, ByRef errormessages As List(Of String)) As String

        text = If(Me.NumericValue = 0, "...", Me.currency.format(Me.NumericValue, buyeraccount.Culture.Code, errormessages))
        If Me.isList Then text &= "&nbsp;*" 'Add a * if it's a list price (or contains a list price element)

    End Function

    Public Function NumericValue() As Decimal
        If IsDBNull(Me.value) Then
            Return 0
        Else
            Return Me.value
        End If

    End Function

    Public Function sqlvalue() As String

        If IsDBNull(Me.value) Then
            Return "null"
        Else
            Return Me.value
        End If

    End Function

    Public Overrides Function ToString() As String

        If IsDBNull(Me.value) Then
            Return "Null"
        Else
            Return System.Convert.ToString(Me.value)
        End If

    End Function

End Class

