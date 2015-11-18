Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents an HP Service Pack service level

Public Class clsServiceLevel
    Implements i_Editable

    Public ID As Integer
    Public MfrCode As String
    Public ServiceLevel As Integer
    Public ServiceLevelGroup As String
    Public SuperGroup As String
    Public Description As clsTranslation
    Public Duration As Integer
    Public PostWarranty As Boolean
    Public Disabled As Boolean
    Public ServiceType As clsServiceType
    Public Response As clsResponse
    Public HpeDmr As Boolean
    Public HpeCdmr As Boolean
    Public HpiAdp As Boolean
    Public HpiDmr As Boolean
    Public HpiTravel As Boolean
    Public HpiTracing As Boolean
    Public HpiTheft As Boolean

    Private Const TABLE As String = "ServiceLevelMap"

    Public Sub New()

    End Sub

    Public Sub New(id As Integer, mfrCode As String, serviceLevel As Integer, serviceLevelGroup As String, superGroup As String, description As clsTranslation, duration As Integer, postWarranty As Boolean,
                   disabled As Boolean, serviceType As clsServiceType, response As clsResponse, hpeDmr As Boolean, hpeCdmr As Boolean, hpiAdp As Boolean, hpiDmr As Boolean, hpiTravel As Boolean, hpiTracing As Boolean, hpiTheft As Boolean)

        Me.ID = id
        Me.MfrCode = mfrCode
        Me.ServiceLevel = serviceLevel
        Me.ServiceLevelGroup = serviceLevelGroup
        Me.SuperGroup = superGroup
        Me.Description = description
        Me.Duration = duration
        Me.PostWarranty = postWarranty
        Me.Disabled = disabled
        Me.ServiceType = serviceType
        Me.Response = response
        Me.HpeDmr = hpeDmr
        Me.HpeCdmr = hpeCdmr
        Me.HpiAdp = hpiAdp
        Me.HpiDmr = hpiDmr
        Me.HpiTravel = hpiTravel
        Me.HpiTracing = hpiTracing
        Me.HpiTheft = hpiTheft

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsServiceLevel(Me.ID, Me.MfrCode, Me.ServiceLevel, Me.ServiceLevelGroup, Me.SuperGroup, Me.Description, Me.Duration, Me.PostWarranty, Me.Disabled, Me.ServiceType, Me.Response, Me.HpeDmr, Me.HpeCdmr, Me.HpiAdp, Me.HpiDmr, Me.HpiTravel, Me.HpiTracing, Me.HpiTheft)

    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = String.Format("delete from {0} where id={1}", TABLE, Me.ID)

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update {0} set mfrCode='{1}', ServiceLevel={2}, ServiceLevelGroup='{3}', SuperGroup='{4}', Duration={5}, PostWarranty={6}, Disabled={7}, ServiceType={8}, Response={9}, DMR={10}, HpeDmr={11}, HpeCdmr={12}, HpiAdp={13}, HpiDmr={14}, HpiTravel={15}, HpiTracining={16}, HpiTheft={17) where ID={18}",
                                 TABLE, Me.MfrCode, Me.ServiceLevel, Me.ServiceLevelGroup, Me.SuperGroup, Me.Duration, Me.PostWarranty, Me.Disabled, Me.ServiceType.ID, Me.Response.ID, Me.HpiAdp, Me.HpeDmr, Me.HpeCdmr, Me.HpiDmr, Me.HpiTravel, Me.HpiTracing, Me.HpiTheft, Me.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Description.text(language)

    End Function

    Public ReadOnly Property Manufacturer As Manufacturer

        Get

            Manufacturer = Manufacturer.Unknown

            If Not String.IsNullOrEmpty(Me.mfrCode) Then
                If String.Equals(Me.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPI
                ElseIf String.Equals(Me.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPE
                End If
            End If

        End Get

    End Property

End Class
