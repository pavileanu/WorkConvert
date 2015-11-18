Imports dataAccess
'There is one master set of attributes... weight, width, height, power consumption, color, flavour etc..
'Insances of this class represent thise attributes themselves (and the localised versions)... they DONT represent the values of those attributes - for that see clsProductAttribute

Public Enum EnumAttributeType
    Numeric
    translation
    rawText
    KVP   'translations - ordered by their numeric value (such as (for example) sets of CPU's

End Enum
Public Class clsAttribute
    Implements i_Editable

    Property ID As Integer
    Property Code As String
    Property Translation As clsTranslation 'of the attribute itself (eg. "width","height","Speed")
    Property Order As Integer
    Property type As EnumAttributeType

    Property Products As Dictionary(Of Integer, clsProduct)

    Dim oCode As String
    Public Sub New()


    End Sub
    Public Sub New(ByVal Code As String, ByVal translation As clsTranslation, ByVal order As Integer)

        'master' Attributes are instatiated with a code, and one translation of their name (probably the english one)
        Me.Translation = translation
        Me.Code = Code
        Me.Order = order
        Products = New Dictionary(Of Integer, clsProduct)
        Dim sql$
        sql$ = "INSERT INTO ATTRIBUTE(code,fk_translation_key_name,[order]) values (" & da.SqlEncode(Code) & "," & translation.Key & "," & order & ");"
        Me.ID = da.DBExecutesql(sql$, True)

        'now add it to the 'master' list of attributes
        iq.Attributes.Add(Me.ID, Me)
        iq.i_attribute_code.Add(Me.Code, Me)

        oCode = Me.Code


    End Sub

    Public Function insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert


        Return New clsAttribute(Me.Code, Me.Translation, Me.Order)

    End Function

    'Public Sub Update()

    '    Dim sql$
    '    sql$ = "UPDATE [attribute] set code=" & da.SqlEncode(Me.Code$) & ",fk_translation_key_name=" & Me.Translation.Key & ",[order]=" & Me.Order & " WHERE id=" & Me.ID
    '    da.DBExecutesql(sql)

    '    'remove from the master index and add back (in case the code has been changed)... which it will have for newly added Attributes (from the intial "" )
    '    iq.i_attribute_code.Remove(oCode)
    '    iq.i_attribute_code.Add(Me.Code, Me)
    '    oCode = Me.Code


    'End Sub

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        'You won't be able to delete an attribute that is in use

        Dim sql$
        sql$ = "DELETE FROM [attribute] where id=" & Me.ID
        da.DBExecutesql(sql$)

        iq.Attributes.Remove(Me.ID)
        iq.i_attribute_code.Remove(Me.Code)


    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Code As String, ByVal translation As clsTranslation, ByVal order As Integer)

        'This version of the constructor ('new' sub)... DOESNT persist (write to the database) the attribute

        Me.ID = ID
        Me.Translation = translation
        Me.Code = Code
        Me.Order = order
        'now add it to the 'master' list of attributes
        iq.Attributes.Add(Me.ID, Me)
        iq.i_attribute_code.Add(Me.Code, Me)

        Products = New Dictionary(Of Integer, clsProduct)

        oCode = Me.Code


    End Sub
    Public Sub update(ByRef Errormessages As List(Of String)) Implements i_Editable.update
        Dim sql$
        sql$ = "UPDATE [attribute] set code=" & da.SqlEncode(Me.Code$) & ",fk_translation_key_name=" & Me.Translation.Key & ",[order]=" & Me.Order & " WHERE id=" & Me.ID
        da.DBExecutesql(sql)

        'remove from the master index and add back (in case the code has been changed)... which it will have for newly added Attributes (from the intial "" )
        iq.i_attribute_code.Remove(oCode)
        iq.i_attribute_code.Add(Me.Code, Me)
        oCode = Me.Code
    End Sub


    Public Function displayName(language As clsLanguage) As String Implements i_Editable.displayName
        Return Me.Translation.text(language)
    End Function
End Class
