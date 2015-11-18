Imports System.Runtime.Serialization
Imports dataAccess

<DataContract>
Public Class clsScreenOverride
    <DataMember>
    Public AccountID As Integer
    <DataMember>
    Public ScreenID As Integer
    <DataMember>
    Public Path As String
    <DataMember>
    Public FieldId As Integer
    <DataMember>
    Public ForceVisibilityTo As Nullable(Of Boolean)
    <DataMember>
    Public ForceOrderTo As Nullable(Of Integer)
    <DataMember>
    Public ForceWidthTo As Nullable(Of Double)
    <DataMember>
    Public ForceSortTo As String
    <DataMember>
    Public ForceFilterTo As String
    <DataMember>
    Public FieldName As String
    <DataMember>
    Public DisplayUnit As clsUnit

  
    Public Sub New(AccountID As Integer, ScreenID As Integer, BranchPath As String, FieldId As Integer, ForceVisibilityTo As Nullable(Of Boolean), ForceOrderTo As Nullable(Of Integer), ForceWidthTo As Nullable(Of Double), ForceSortTo As String, ForceFilterTo As String,DisplayUnit As clsUnit)
        Me.AccountID = AccountID
        Me.FieldId = FieldId
        Me.ForceVisibilityTo = ForceVisibilityTo
        Me.ForceOrderTo = ForceOrderTo
        Me.ScreenID = ScreenID
        Me.Path = BranchPath
        Me.ForceWidthTo = ForceWidthTo
        Me.ForceSortTo = ForceSortTo
        Me.ForceFilterTo = ForceFilterTo
        Me.DisplayUnit = DisplayUnit
        iq.ScreenOverrides.Add(Me)
    End Sub

    Public Sub New()

    End Sub

    Public Function Update() As Boolean

        Dim sql$
        sql$ = "Update AccountScreenOverride set ForceVisibilityTo = " & If(Me.ForceVisibilityTo Is Nothing, "NULL", da.SqlEncode(Me.ForceVisibilityTo)) & ",ForceOrderTo = " & If(Me.ForceOrderTo Is Nothing, "NULL", da.SqlEncode(Me.ForceOrderTo)) & ",ForceWidthTo = " & If(Me.ForceWidthTo Is Nothing, "NULL", da.SqlEncode(Me.ForceWidthTo)) & ", ForceSortTo = " & If(Me.ForceSortTo Is Nothing, "NULL", da.SqlEncode(Me.ForceSortTo)) & ",ForceFilterTo=" & If(Me.ForceFilterTo Is Nothing, "NULL", da.SqlEncode(Me.ForceFilterTo)) & " , FK_DisplayUnit_ID = " & If(Me.DisplayUnit Is Nothing, "NULL", Me.DisplayUnit.ID) & " where Path = " & da.SqlEncode(Me.Path) & " AND [FK_Screen_Id]=" & Me.ScreenID & " and FK_Account_Id=" & Me.AccountID & " AND FK_Field_Id = " & Me.FieldId
        Return da.DBExecutesql(sql$, False) > 0
    End Function

    Public Function Insert() As Boolean
        Dim sql$
        sql$ = "INSERT INTO AccountScreenOverride (FK_Account_Id,FK_Screen_Id,Path,FK_Field_Id,ForceVisibilityTo,ForceOrderTo,ForceWidthTo,ForceSortTo,ForceFilterTo,FK_DisplayUnit_ID) VALUES (" & Me.AccountID & "," & Me.ScreenID & "," & If(Me.Path Is Nothing, "NULL", da.SqlEncode(Me.Path)) & "," & Me.FieldId & "," & If(Me.ForceVisibilityTo Is Nothing, "NULL", da.SqlEncode(Me.ForceVisibilityTo)) & "," & If(Me.ForceOrderTo Is Nothing, "NULL", da.SqlEncode(Me.ForceOrderTo)) & "," & If(Me.ForceWidthTo Is Nothing, "NULL", da.SqlEncode(Me.ForceWidthTo)) & "," & If(Me.ForceSortTo Is Nothing, "NULL", da.SqlEncode(Me.ForceSortTo)) & "," & If(Me.ForceFilterTo Is Nothing, "NULL", da.SqlEncode(Me.ForceFilterTo)) & "," & If(Me.DisplayUnit Is Nothing, "NULL", Me.DisplayUnit.ID) & ")"
        Return da.DBExecutesql(sql$, False) > 0
    End Function



End Class
