Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents a localized legal statement

Public Class clsLegal
    Implements i_Editable

    Public ID As Integer
    Public Code As String
    Public Translation As clsTranslation

    Public Sub New()

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Code As String, ByVal Translation As clsTranslation)

        Me.ID = ID
        Me.Code = Code
        Me.Translation = Translation

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsLegal(Me.ID, Me.Code, Me.Translation)

    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = "delete from [Legal] where id=" & Me.ID

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update [Legal] set Code='{0}' where ID={1}", Me.Code, Me.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Translation.text(language)

    End Function

End Class
