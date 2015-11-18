Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents an HPE Care Pack service type level

Public Class clsServiceType
    Implements i_Editable

    Public ID As Integer
    Public mfrCode As String
    Public Title As clsTranslation
    Public Description As clsTranslation
    Public ServiceTypeDefault As Boolean

    Private Const TABLE As String = "ServiceType"

    Public Sub New()

    End Sub

    Public Sub New(id As Integer, mfrCode As String, title As clsTranslation, description As clsTranslation, serviceTypeDefault As Boolean)

        Me.ID = id
        Me.mfrCode = mfrCode
        Me.Title = title
        Me.Description = description
        Me.ServiceTypeDefault = serviceTypeDefault

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsServiceType(Me.ID, Me.mfrCode, Me.Title, Me.Description, Me.ServiceTypeDefault)

    End Function

    Public Sub Delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = String.Format("delete from {0} where id={1}", TABLE, Me.ID)

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update {0} set mfrCode='{1}', IsDefault={2} where ID={3}", TABLE, Me.mfrCode, Me.ServiceTypeDefault, Me.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Title.text(language)

    End Function

End Class
