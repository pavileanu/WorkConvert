Imports dataAccess

Public Class clsPromo
    Implements i_Editable

    Property Id As Integer
    Property Code As String
    Property Description As clsTranslation
    Property FieldProperty_Filter As String
    Property FieldProperty_Value As String
    Property Region As clsRegion

    Public Sub New(ID As Integer, Code As String, Description As clsTranslation, Region As clsRegion, FieldProperty_Filter As String, FieldProperty_Value As String, SystemType As String)
        Me.Id = ID
        Me.Code = Code
        Me.Description = Description
        Me.FieldProperty_Filter = FieldProperty_Filter
        Me.Region = Region
        Me.FieldProperty_Value = FieldProperty_Value

        iq.Promos.Add(Me.Id, Me)
        AddRegion(Region)
        AddSystemType(SystemType)
    End Sub

    Public Sub loadRegionIteration(Region As clsRegion)
        If Not iq.i_PromoRegions.ContainsKey(Region) Then iq.i_PromoRegions.Add(Region, New List(Of clsPromo))
        If Not iq.i_PromoRegions(Region).Contains(Me) Then iq.i_PromoRegions(Region).Add(Me)

        For Each r In Region.Children.Values
            loadRegionIteration(r)
        Next
    End Sub
    Public Sub AddRegion(region As clsRegion)
        loadRegionIteration(region)
    End Sub
    Public Sub AddSystemType(systype As String)
        If Not iq.i_PromoSystemTypes.ContainsKey(Me) Then iq.i_PromoSystemTypes.Add(Me, New List(Of String))
        If Not iq.i_PromoSystemTypes(Me).Contains(systype) Then iq.i_PromoSystemTypes(Me).Add(systype)
    End Sub


    Public Sub delete(ByRef Errormessages As List(Of String)) Implements i_Editable.delete

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName
        Return Description.text(Language)
    End Function

    Public Function Insert(ByRef Errormessages As List(Of String)) As Object Implements i_Editable.Insert
        Return Nothing
    End Function

    Public Sub update(ByRef Errormessages As List(Of String)) Implements i_Editable.update

        'UNFINISHED / TESTED

        Dim sql$
        sql$ = "UPDATE [promo] set code=" & da.SqlEncode(Me.Code$) & ",fk_translation_key_description=" & Me.Description.Key & " WHERE id=" & Me.Id
        da.DBExecutesql(sql)


    End Sub
End Class

