
Imports System.Web.Http
Imports System.Net.Http

Public Class clsGenericAjaxRequest
    Inherits HttpRequestMessage

    Public ScreenId As Integer
    Public lid As UInt64
    Public elid As UInt64
    Public BranchPath As String
    Public ScreenTitle As String
    Public ActionId As Integer
    Public SourceFieldId As Integer
    Public DestinationFieldId As Integer
    Public SysType As String
    Public QuoteId As Int32
    Public ParentId As Int32
End Class
