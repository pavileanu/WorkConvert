Imports System.Linq
Public Class clsFlatList
    Public items As List(Of clsFlatListItem)
    ''' <summary>
    ''' To check if item has been already been clsQuoteItem with IsPreinstalled to false.
    ''' </summary>
    ''' <param name="quoteitem">An instance of clsQuoteItem.</param>
    ''' <returns>A boolean value true/ false.</returns>
    ''' <remarks></remarks>
    Public Function DoesNoneInstalledExist(quoteitem As clsQuoteItem) As Boolean

        Dim result As Boolean = False


        result = items.Where(Function(x) x.QuoteItem.IsPreInstalled = False And x.QuoteItem.Path = quoteitem.Path).Select(Function(x) x).Count > 0
        Return result
    End Function

    Public Function PSV(Product As clsProduct, SKUVariant As clsVariant) As clsFlatListItem

        'becuase more than one variant can exist on a branch
        'AND a variant can be on many branches 
        'we're wanting to group by distinct Variant/Path
        'returns the the item with the Product and SKU Variant as specified (or nothing it no item exists in the flatlist)

        PSV = Nothing
        For Each i In items
            If i.QuoteItem.Branch.Product Is Product And i.QuoteItem.SKUVariant Is SKUVariant Then
                Return i
            End If
        Next

    End Function

    Public Sub New()
        items = New List(Of clsFlatListItem)
    End Sub

End Class
