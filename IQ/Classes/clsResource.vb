Option Explicit On
Option Strict On

Imports dataAccess
Imports System.IO

' Represents a Resource file item for display on the Resources page

Public Class clsResource
    Implements i_Editable

    Public ID As Integer
    Public Description As String
    Public Type As String
    Public Code As String
    Public Title As clsTranslation
    Public Region As clsRegion
    Public Language As clsLanguage
    Public SellerChannel As clsChannel
    Public MfrCode As String
    Public Order As Integer
    Public CategoryId As Integer
    Public Embed As Boolean

    Public Sub New()

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Description As String, ByVal Type As String, ByVal Code As String, ByVal Title As clsTranslation, ByVal Region As clsRegion,
                   ByVal Language As clsLanguage, ByVal SellerChannel As clsChannel, ByVal MfrCode As String, ByVal Order As Integer, ByVal CategoryId As Integer, ByVal Embed As Boolean)

        Me.ID = ID
        Me.Description = Description
        Me.Type = Type
        Me.Code = Code
        Me.Title = Title
        Me.Region = Region
        Me.Language = Language
        Me.SellerChannel = SellerChannel
        Me.MfrCode = MfrCode
        Me.Order = Order
        Me.CategoryId = CategoryId
        Me.Embed = Embed

    End Sub

    Public ReadOnly Property Manufacturer As Manufacturer

        Get

            Manufacturer = Manufacturer.Unknown

            If Not String.IsNullOrEmpty(Me.MfrCode) Then
                If String.Equals(Me.MfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPI
                ElseIf String.Equals(Me.MfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPE
                End If
            End If

        End Get

    End Property

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsResource(Me.ID, Me.Description, Me.Type, Me.Code, Me.Title, Me.Region, Me.Language, Me.SellerChannel, Me.MfrCode, Me.Order, Me.CategoryId, Me.Embed)

    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$ = "delete from [Resource] where id=" & Me.ID

        da.DBExecutesql(sql$)

    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$ = String.Format("update [Resource] set Code='{0}', [Description]='{1}', [FK_Resource_Category_ID]={2}, [Type]='{3}', [Code]='{4}', [FK_Region_ID]={5}, [FK_Language_ID]={6}, [FK_SellerChannel_ID]={7}, [mfrCode]='{8}', [Order]={9}, [Embed]={10} where ID={11}", _
                                 Me.Code, Me.Description, Me.CategoryId, Me.Type, Me.Code, Me.Region.ID, Me.Language.ID, Me.SellerChannel.ID, Me.MfrCode, Me.Order, Me.Embed, Me.ID)

        da.DBExecutesql(sql$, False)

    End Sub

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Description

    End Function

End Class
