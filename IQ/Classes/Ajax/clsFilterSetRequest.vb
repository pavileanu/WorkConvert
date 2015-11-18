Imports System.Web.Http
Imports System.Net.Http

Public Class clsFilterSetRequest
    Inherits HttpRequestMessage

    Public lid As UInt64
    Public ScreenID As Integer
    Public Fields As List(Of clsFieldSetRequestDetail)
End Class
Public Class clsFieldSetRequestDetail
    Inherits HttpRequestMessage

    Public FieldId As Integer
    Public DefaultFilter As String
    Public TranslationGroup As String
    Public FilterType As String
    Public Order As Integer
    Public Enabled As Boolean
End Class
