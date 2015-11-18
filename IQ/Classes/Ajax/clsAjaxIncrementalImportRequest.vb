Imports System.Net.Http

Public Class clsAjaxIncrementalImportRequest
    Inherits HttpRequestMessage

    Public Property lid As UInt64
    Public Property elid As UInt64
    Public Property SKUList As String
    Public Property atPoint As Int32

    Public Property SubmitList As List(Of System.Collections.Generic.KeyValuePair(Of Integer, Boolean))

End Class
