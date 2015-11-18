
Public Class clsCustomerContext
    Private icmsLocation As String
    Private taxType As String
    Private warhouseLocation As String

    Public Property Location() As String
        Get
            Return icmsLocation
        End Get
        Set(ByVal value As String)
            icmsLocation = value
        End Set
    End Property

    Public Property Tax() As String
        Get
            Return taxType
        End Get
        Set(ByVal value As String)
            taxType = value
        End Set
    End Property

    Public Property WareHouse() As String
        Get
            Return warhouseLocation
        End Get
        Set(ByVal value As String)
            warhouseLocation = value
        End Set
    End Property

End Class
