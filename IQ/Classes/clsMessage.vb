Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents a message displayed to the user - e.g. the banner message on the Sign In screen

Public Class clsMessage

    Implements i_Editable

    Public ID As Integer
    Public Code As String
    Public Translation As clsTranslation
    Public ValidFrom As DateTime
    Public ValidTo As DateTime
    Public Enabled As Boolean
    Public ChannelID As Integer

    Private DATEFORMAT As String = "dd-MMM-yyyy"

    Public Sub New()

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Code As String, ByVal Translation As clsTranslation, ByVal ValidFrom As DateTime, ByVal ValidTo As DateTime, ByVal Enabled As Boolean, ByVal ChannelID As Integer)

        Me.ID = ID
        Me.Code = Code
        Me.Translation = Translation
        Me.ValidFrom = ValidFrom
        Me.ValidTo = ValidTo
        Me.Enabled = Enabled
        Me.ChannelID = ChannelID

    End Sub

    Public Sub New(ByVal Code As String, ByVal Translation As clsTranslation, ByVal ValidFrom As DateTime, ByVal ValidTo As DateTime, ByVal Enabled As Boolean, ByVal ChannelID As Integer)

        Me.Code = Code
        Me.Translation = Translation
        Me.ValidFrom = ValidFrom
        Me.ValidTo = ValidTo
        Me.Enabled = Enabled
        Me.ChannelID = ChannelID

        Dim sql$ = String.Format("insert into [message](Code, FK_Translation_key_Name, ValidFrom, ValidTo, FK_Channel_ID, Enabled) values ('{0}', {1}, '{2}', '{3}', {4}, {5})", _
                                 Me.Code, Me.Translation.Key, Me.ValidFrom.ToString(DATEFORMAT), Me.ValidTo.ToString(DATEFORMAT), 1, If(Me.Enabled, 1, 0), Me.ChannelID)

        Me.ID = da.DBExecutesql(sql$, True)

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsMessage(Me.Code, Me.Translation, Me.ValidFrom, Me.ValidTo, Me.Enabled, Me.ChannelID)

    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = "delete from [message] where id=" & Me.ID

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update [message] set ValidFrom='{0}', ValidTo='{1}', Enabled={2} where ID={3}", _
                                 Me.ValidFrom.ToString(DATEFORMAT), Me.ValidTo.ToString(DATEFORMAT), If(Me.Enabled, 1, 0), Me.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Translation.text(language)

    End Function

End Class
