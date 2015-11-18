Option Strict On
Option Explicit On
Imports dataAccess

Public Class clsCulture
    Property ID As Integer
    Property Code As String
    Property Name As String
    Public Sub New(ID As Integer, code As String, name As String)
        Me.ID = ID
        Me.Code = code
        Me.Name = name
        iq.Cultures.Add(ID, Me)
        iq.i_culture_code.Add(code, Me)
    End Sub
End Class
