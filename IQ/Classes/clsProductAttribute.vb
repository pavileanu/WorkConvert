Imports dataAccess
Imports System.Xml

Public Class clsProductAttribute
    Implements i_Editable

    Property ID As Integer
    Property Product As clsProduct
    Property Attribute As clsAttribute
    Property NumericValue As Single
    Property Translation As clsTranslation
    '    Property rawtext As String 'some things such as part numbers
    Property Unit As clsUnit  'what units is the (numeric) value expressed in GB/TB/MB/Mhz/Ghz/feet/Pounds/Centigrade etc
    Property deleted As Boolean

    Dim oCode As String
    Dim oProduct As clsProduct

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Try
            Dim sql$
            sql$ = "DELETE FROM [ProductAttribute] WHERE ID=" & Me.ID
            da.DBExecutesql(sql$)

            Me.Product.Attributes.Remove(Me.ID)
            Me.Product.i_Attributes_Code.Remove(Me.Attribute.Code)

        Catch ex As System.Exception

            errorMessages.Add(ex.Message)
        End Try

    End Sub

    Public Sub writeXML(xw As XmlTextWriter)

        xw.WriteStartElement("productAttribute")
        xw.WriteStartAttribute("id")
        xw.WriteString(Me.ID.ToString)
        xw.WriteEndAttribute()

        xw.WriteStartAttribute("code")
        xw.WriteString(Me.Attribute.Code.ToString)
        xw.WriteEndAttribute()

        xw.WriteStartAttribute("numericValue")
        xw.WriteString(Me.NumericValue.ToString)
        xw.WriteEndAttribute()

        If Me.Unit IsNot Nothing Then
            xw.WriteStartAttribute("unit")
            xw.WriteString(Me.Unit.Code.ToString)
            xw.WriteEndAttribute()
        End If

        If Me.Translation IsNot Nothing Then
            xw.WriteStartAttribute("text")
            xw.WriteString(Me.Translation.text(English))
            xw.WriteEndAttribute()
        End If

        xw.WriteEndElement() '/productAttribute

        ' Return String.Format("<productAttribute id='{0}' code='{1}'><numericValue>{2}</numericValue><units>{3}</units><text>{4}</text></productAttribute>", _
        ' Me.ID, _
        ' Me.Attribute.Code, _
        ' Me.NumericValue, _
        ' If(Me.Unit Is Nothing, "none", Me.Unit.Code), _
        ' If(Me.Translation IsNot Nothing, xmlEncode(Me.Translation.text(English)), "")
        '         )

    End Sub
    Public Function displayName(language As clsLanguage) As String Implements i_Editable.displayName

        If Not Translation Is Nothing Then
            displayName = Me.Attribute.Code & " " & Translation.text(language)
        Else
            displayName = Me.Attribute.Code & " " & NumericValue & " "
            If Not Me.Unit Is Nothing Then
                If Unit IsNot iq.i_unit_code("txt") Then displayName &= Me.Unit.DisplayName(language)
            End If
        End If

    End Function

    Public Function displayNameNoCode(language As clsLanguage) As String

        If Not Translation Is Nothing Then
            displayNameNoCode = Translation.text(language)
        Else
            displayNameNoCode = NumericValue & " "
            If Not Me.Unit Is Nothing Then
                If Unit IsNot iq.i_unit_code("txt") Then displayNameNoCode &= Me.Unit.DisplayName(language)
            End If
        End If

    End Function

    Public Sub update(ByRef errormessages As List(Of String)) Implements i_Editable.update

        Dim sql$

        sql$ = "UPDATE [ProductAttribute] SET fk_product_id=" & Me.Product.ID & ","
        sql$ &= "fk_attribute_id=" & Me.Attribute.ID & ","
        sql$ &= "numericvalue=" & Me.NumericValue & ","
        sql$ &= "fk_unit_id=" & Me.Unit.ID & ","
        sql$ &= "fk_translation_key_text=" & TranslationKey(Me.Translation)
        sql$ &= ",deleted=" & IIf(Me.deleted, 1, 0)

        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql$)

        'update the index and dictionary in the parent product (of this productAttribute) (in case we changed the code, or the product .. ie, reparented this productAttribute)
        Me.Product.i_Attributes_Code.Remove(Me.oCode)
        If Not Me.Product.i_Attributes_Code.ContainsKey(Me.Attribute.Code) Then
            Me.Product.i_Attributes_Code.Add(Me.Attribute.Code, New List(Of clsProductAttribute))
        End If
        Me.Product.i_Attributes_Code(Me.Attribute.Code).Add(Me)
        Me.oCode = Me.Attribute.Code


    End Sub
    Public Sub New()

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert
        Return New clsProductAttribute(Me.Product, Me.Attribute, Me.NumericValue, Me.Unit, Me.Translation)

    End Function


    Public Sub New(Product As clsProduct, attCode As String, numericValue As Single, unitCode As String, EnglishText As String, pawc As DataTable, tlwc As DataTable, ByRef tlkey As Integer)

        'additional ovload contstructor for *ProductAttribute*  for cleaner code - looks up the attribute and created the translation internally
        'constructor chaining . .  (calls the (other) constructor with the matching signature)
        Me.New(Product, iq.i_attribute_code(attCode), numericValue, iq.i_unit_code(unitCode), IIf(EnglishText = "", Nothing, iq.AddTranslation(EnglishText, English, "PA-" & attCode, 0, tlwc, tlkey, False)), pawc)

    End Sub

    Public Sub New(ByVal Product As clsProduct, ByVal Attribute As clsAttribute, ByVal NumericValue As Single, ByVal Unit As clsUnit, ByVal Translation As clsTranslation, Optional pawc As DataTable = Nothing, Optional InMemoryOnly As Boolean = False)

        Me.Product = Product
        Me.Attribute = Attribute
        Me.NumericValue = NumericValue
        Me.Translation = Translation
        Me.Unit = Unit
        '   Me.ShowUnderBranches  = ShowUnder 'Comma seperated list of branchID 
        '   Me.HideUnderBranches = HideUnder

        'A Product *CAN* have more than one attribute of the same type
        If Not Me.Product.i_Attributes_Code.ContainsKey(Me.Attribute.Code) Then
            Me.Product.i_Attributes_Code.Add(Me.Attribute.Code, New List(Of clsProductAttribute))
        End If

        ' Me.Product.i_Attributes_Code(Me.Attribute.Code).Add(Me)

        If Not InMemoryOnly Then
            If pawc Is Nothing Then
                Dim sql$
                sql$ = "INSERT INTO PRODUCTATTRIBUTE (FK_Product_id,FK_Attribute_id,NumericValue,FK_Translation_key_Text,fk_Unit_id) "
                sql$ &= " values (" & Me.Product.ID & "," & Me.Attribute.ID & "," & Me.NumericValue.ToString & "," & TranslationKey(Me.Translation) & "," & Me.Unit.ID & ");"

                Me.ID = da.DBExecutesql(sql$, True)

            Else

                Dim row As System.Data.DataRow
                row = pawc.NewRow()
                row("FK_product_id") = Product.ID
                row("FK_attribute_id") = Attribute.ID
                row("numericvalue") = NumericValue
                If Me.Translation Is Nothing Then
                    row("fk_translation_key_text") = DBNull.Value
                Else
                    row("fk_translation_key_text") = Me.Translation.Key
                End If

                row("fk_unit_ID") = Unit.ID
                row("deleted") = False
                pawc.Rows.Add(row)

            End If
        End If

        If Attribute.Code.ToLower = "mfrsku" Then Stop

        'add this attribute to the product we're making it against
        If Not Me.ID = 0 Then
            Product.Attributes.Add(Me.ID, Me)
        End If

        'Index products by attribute (for attribute finder speed) - ML
        If Not Me.Attribute.Products.ContainsKey(Me.Product.ID) Then Me.Attribute.Products.Add(Me.Product.ID, Me.Product)

        'add this attribute to the product we're making it against
        If Not Product.i_Attributes_Code.ContainsKey(Me.Attribute.Code) Then
            Product.i_Attributes_Code.Add(Me.Attribute.Code, New List(Of clsProductAttribute))
        End If

        Product.i_Attributes_Code(Attribute.Code).Add(Me)

        Me.oProduct = Me.Product
        Me.oCode = Me.Attribute.Code

    End Sub

    Public Function Clone(ontoProduct As clsProduct) As clsProductAttribute

        'returns an independent copy of the clsProductAttribute attached instead to Product - with an independent copy of the translation
        Dim tl As clsTranslation = Nothing
        If Me.Translation IsNot Nothing Then tl = Me.Translation.clone
        Clone = New clsProductAttribute(ontoProduct, Me.Attribute, Me.NumericValue, Me.Unit, tl)

    End Function

    Public Sub New(ByVal id As Integer, ByVal Product As clsProduct, ByVal Attribute As clsAttribute, ByVal NumericValue As Single, ByVal Unit As clsUnit, ByVal Translation As clsTranslation)

        Me.ID = id
        Me.Product = Product
        Me.Attribute = Attribute
        Me.NumericValue = NumericValue
        Me.Translation = Translation
        Me.Unit = Unit

        'Backward compatability for SKU attribute
        'If Me.Product.SKU = "" And Me.Attribute.Code.ToLower = "mfrsku" Then
        '    Product.SKU = Translation.text(English)
        '    If Not iq.i_SKU.ContainsKey(Product.SKU) Then 'this is UGLY - but San Switches, Tape dirves etc have two products at the mo
        '        iq.i_SKU.Add(Product.SKU, Product)
        '    End If
        'End If


        'If Attribute.Code.ToLower = "mfrsku" Then Stop

        '*OBSOLETED* - Sku is not a native property of the product - we should NOT be making mfrSku Product attributes any more
        ''Add this SKU to the SKU-product index
        'If Attribute.Code.ToLower = "MfrSKU".ToLower Then
        '    Dim sku$

        '    If Product.SKU = "" Then Stop
        '    sku$ = Me.Translation.text(English)
        '    If Not iq.i_SKU.ContainsKey(sku$) Then
        '        'product hasn't (previously) had its sku set
        '        iq.i_SKU.Add(sku$, Product)
        '    End If
        'End If


        'add this attribute to the product we're making it against
        If Not Product.i_Attributes_Code.ContainsKey(Me.Attribute.Code) Then
            Product.i_Attributes_Code.Add(Me.Attribute.Code, New List(Of clsProductAttribute))
        End If

        Product.i_Attributes_Code(Attribute.Code).Add(Me)
        Product.Attributes.Add(Me.ID, Me)

        'Index products by attribute (for attribute finder speed) - ML
        If Not Me.Attribute.Products.ContainsKey(Me.Product.ID) Then Me.Attribute.Products.Add(Me.Product.ID, Me.Product)


        Me.oProduct = Product
        Me.oCode = Me.Attribute.Code


    End Sub


End Class
