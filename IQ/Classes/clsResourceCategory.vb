Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents a Resource category for display on the Resources page
' TODO: needs to implement i_Editable

Public Class clsResourceCategory

    Public ID As Integer
    Public Name As String
    Public Translation As clsTranslation
    Public Order As Integer
    Public Resources As List(Of clsResource)

    Public Sub New()

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Name As String, ByVal Translation As clsTranslation, ByVal Order As Integer)

        Me.ID = ID
        Me.Name = Name
        Me.Translation = Translation
        Me.Order = Order

    End Sub

End Class
