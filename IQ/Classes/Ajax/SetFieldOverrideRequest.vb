Imports System.Web.Http
Imports System.Net.Http

Public Class SetFieldOverrideRequest
    Inherits HttpRequestMessage
    Public lid As UInt64
    Public BranchPath As String
    Public ScreenId As Integer
    Public FieldId As Integer
    Public ForceVisibilityTo As Boolean?
    Public ForceOrderTo As Integer?
    Public ForceWidthTo As Double?
    Public ForceSortTo As String
    Public ForceFilterTo As String
End Class
