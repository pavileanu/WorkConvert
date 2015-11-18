Option Strict On
Imports System.Threading.Tasks

Public Class AuditLog
    Private Shared _instance As AuditLog
    Public Shared ReadOnly Property Instance As AuditLog
        Get
            If _instance Is Nothing Then
                _instance = New AuditLog()
            End If
            Return _instance
        End Get
    End Property

    Private log As Queue(Of clsAuditEntry) = New Queue(Of clsAuditEntry)()
    Public TAction As Action(Of clsAuditEntry) = Sub(cae As clsAuditEntry)
                                                     cae.Save()
                                                 End Sub


    Private thisThread As System.Threading.Thread
   
    'Default Add for page loads in clsPageLogging
    Public Sub Add(lid As UInt64, Action As String, SourcePath As String, TargetPath As String, errormessages As List(Of String), ex As Exception, PageName As String, SourceURL As String, TimeToLoad As Double, HttpRequestMethod As String, UrlReferrer As String)
        Dim t = Task.Factory.StartNew(Sub() TAction(New clsAuditEntry() With {.lid = lid, .Action = Action, .SourcePath = SourcePath, .TargetPath = TargetPath, .DateTime = DateTime.Now, .Message = String.Join(",", errormessages), .SecondaryMessage = If(ex IsNot Nothing, ex.Message, String.Empty), .SourceURL = SourceURL, .TimeToLoad = TimeToLoad, .PageName = PageName, .HttpRequestMethod = HttpRequestMethod, .UrlReferrer = UrlReferrer}))
    End Sub

    Public Sub Add(lid As UInt64, Action As String, errormessages As List(Of String), UrlReferrer As String)
        Dim t = Task.Factory.StartNew(Sub() TAction(New clsAuditEntry() With {.lid = lid, .Action = Action, .DateTime = DateTime.Now, .Message = String.Join(",", errormessages), .UrlReferrer = UrlReferrer}))
    End Sub

    Public Sub Add(Type As AuditType, Message As String, PageName As String, Optional lid As UInt64? = Nothing)
        Dim t = Task.Factory.StartNew(Sub() TAction(New clsAuditEntry() With {.AuditType = Type.ToString(), .DateTime = DateTime.Now, .PageName = PageName, .Message = Message, .lid = lid.Value}))
    End Sub

    'Sub sendQueue()
    '    'Make this async with more time - ML
    '    'Do
    '    While log.Count > 0
    '        Try
    '            Dim a = log.Peek()
    '            If a IsNot Nothing Then
    '                If a.Save() And log.Count > 0 Then log.Dequeue()
    '            End If
    '            System.Threading.Thread.Sleep(100)
    '        Catch ex As Exception
    '            ErrorLog.Add(ex)
    '        End Try
    '    End While

    '    'System.Threading.Thread.Sleep(10000)
    '    'Loop
    'End Sub

    Sub MarkUndone(AuditId As Integer, lid As UInt64, Action As String, errormessages As List(Of String), URLReferer As String)
        Dim t = Task.Factory.StartNew(Sub() TAction(New clsAuditEntry() With {.AuditId = AuditId, .DBMethod = 1, .ActionUndone = True}))
        Add(lid, Action, errormessages, URLReferer)
    End Sub
End Class
Public Enum AuditType
    Debug
    [Error]
    Warning
    Information
    Editor
End Enum
