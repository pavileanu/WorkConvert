
Public Class clsBand
    Public min As Int64
    Public max As Int64
    Public Survivors As Integer 'The number of values falling in this band

    Public Sub New(min As Int64, max As Int64, Survivors As Integer)
        Me.min = min
        Me.max = max
        Me.Survivors = Survivors
    End Sub

    Public Function contains(numericValue As Int64) As Boolean
        contains = False
        If numericValue >= Me.min And numericValue <= Me.max Then
            contains = True
        End If
    End Function

    Public Sub Stretch()

        'do some rounding and overlapping here on the Max/Min

        Me.min = CType(Math.Ceiling(Me.min / 1000) * 1000, Int64)
        Me.max = CType(Math.Floor(Me.max / 1000) * 1000, Int64)


    End Sub

    ''' <summary>
    ''' Compares the Min and Max of this band to those values specified for the LE and GE filters (in the dictionary, for the field provided - to determin with this band is the currently selected one
    ''' </summary>
    ''' <param name="fld"></param>
    ''' <param name="filters"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function isSelected(fld As clsField, filters As Dictionary(Of clsField, Dictionary(Of clsFilter, List(Of Int64)))) As Boolean

        isSelected = False

        If filters.ContainsKey(fld) Then
            Dim ge As clsFilter = iq.i_Filters_Code("GE")
            Dim le As clsFilter = iq.i_Filters_Code("LE")

            With filters(fld)

                If .ContainsKey(ge) Then
                    If .ContainsKey(le) Then
                        Dim mi = .Item(ge).IndexOf(Me.min)
                        If mi = -1 Then Return False
                        If .Item(le)(mi) = Me.max Then Return True
                        'If .Item(ge).First() = Me.min And .Item(le).First() = Me.max Then Return True 'ML - have guessed here that there will only be one min and one max, will need to enforce that this is the case - TODO
                    End If
                End If
            End With
        End If
    End Function

    Public Overloads Function Equals(obj As clsBand) As Boolean
        Return max = CULng(obj.max) AndAlso min = obj.min
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return max.GetHashCode() + min.GetHashCode()
    End Function

End Class

