Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents a collection of OS ROK attributes

Public Class clsROKAttribute

    Public ID As Integer
    Public OsCode As String
    Public Code As String
    Public Translation As clsTranslation

    Public Sub New(ByVal ID As Integer, ByVal OsCode As String, ByVal Code As String, ByVal Translation As clsTranslation)

        Me.ID = ID
        Me.OsCode = OsCode
        Me.Code = Code
        Me.Translation = Translation

    End Sub

    Public Sub update()

        Dim sql$ = "UPDATE ROKAttributes set fk_translation_key_name=" & Me.Translation.Key & " WHERE id = " & Me.ID
        da.DBExecutesql(sql)

    End Sub

End Class
