Imports dataAccess
Imports System.Runtime.Serialization

Public Class NullableDate

    Public value As Object

    Public Sub New()
        Me.value = DBNull.Value
    End Sub

    Public Sub New(v As Object)
        If IsDBNull(v) Then
            Me.value = DBNull.Value
        Else
            Me.value = v.ToString
        End If
    End Sub

    Public Function DisplayValue() As String
        If IsDBNull(Me.value) Then
            Return "-"
        Else
            Return Me.value.ToString
        End If

    End Function
    Public Function sqlValue() As String

        If IsDBNull(Me.value) Then
            Return "null"
        Else
            Return da.UniversalDate(Me.value)
        End If

    End Function

End Class

<DataContract>
<System.Runtime.Serialization.KnownType(GetType(nullableString))>
Public Class nullableString
    <DataMember>
    Public Property v As Object
        Get
            Return If(value Is DBNull.Value, Nothing, value)
        End Get
        Set(value As Object)
            Me.value = If(value Is Nothing, DBNull.Value, value)
        End Set
    End Property
    Public value As Object

    Public Sub New()
        Me.value = DBNull.Value
    End Sub

    Public Sub New(v As Object)
        If IsDBNull(v) Then
            Me.value = DBNull.Value
        Else
            Me.value = v.ToString
        End If
    End Sub

    Public Function DisplayValue() As String
        If IsDBNull(Me.value) Then
            Return "-"
        Else
            Return Me.value.ToString
        End If

    End Function
    Public Function sqlValue() As String

        If IsDBNull(Me.value) Then
            Return "null"
        Else
            Return da.SqlEncode(Me.value.ToString)
        End If

    End Function


End Class

