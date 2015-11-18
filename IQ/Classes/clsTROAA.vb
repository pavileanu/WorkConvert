Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents an HPE Care Pack Top Recommended Option/Auto Add

Public Class clsTROAA
    Implements i_Editable

    Public ID As Integer
    Public SysFamily As String
    Public SlotTypeCode As Integer
    Public ServiceLevelID As Integer
    Public DisplayOrder As Integer
    Public ServiceLevel As clsServiceLevel

    Private Const TABLE As String = "TROAA"

    Public Sub New()

    End Sub

    Public Sub New(id As Integer, sysFamily As String, slotTypeCode As Integer, serviceLevelID As Integer, displayOrder As Integer, serviceLevel As clsServiceLevel)

        Me.ID = id
        Me.SysFamily = sysFamily
        Me.SlotTypeCode = slotTypeCode
        Me.ServiceLevelID = serviceLevelID
        Me.DisplayOrder = displayOrder
        Me.ServiceLevel = serviceLevel

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsTROAA(Me.ID, Me.SysFamily, Me.SlotTypeCode, Me.ServiceLevelID, Me.DisplayOrder, Me.ServiceLevel)

    End Function

    Public Sub Delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = String.Format("delete from {0} where id={1}", TABLE, Me.ID)

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update {0} set SysFamily='{1}', SlotTypeCode={2}, ServiceLevel={3}, DisplayOrder={4}, FK_ServiceLevelMap_ID={5} where ID={6}", TABLE, Me.SysFamily, Me.SlotTypeCode, Me.ServiceLevelID, Me.DisplayOrder, Me.ServiceLevel.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return String.Format("{0}-{1}-{2}", Me.SysFamily, Me.SlotTypeCode, Me.ServiceLevelID)

    End Function

End Class
