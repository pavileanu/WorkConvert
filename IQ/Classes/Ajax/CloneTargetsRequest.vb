Imports System.Web.Http
Imports System.Net.Http

Public Class CloneTargetsRequest
    Inherits HttpRequestMessage

    Public ScreenId As Integer
    Public Path As String
    Public Targets As List(Of String)
    Public lid As UInt64
    Public Level As String
    Public LevelValue As String

End Class
