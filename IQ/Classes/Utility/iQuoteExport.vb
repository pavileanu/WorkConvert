﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.34014
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict Off
Option Explicit On

Imports System.Xml.Serialization

'
'This source code was auto-generated by xsd, Version=4.0.30319.18020.
'

'''<remarks/>
<System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),  _
 System.SerializableAttribute(),  _
 System.Diagnostics.DebuggerStepThroughAttribute(),  _
 System.ComponentModel.DesignerCategoryAttribute("code"),  _
 System.Xml.Serialization.XmlTypeAttribute(AnonymousType:=true),  _
 System.Xml.Serialization.XmlRootAttribute([Namespace]:="", IsNullable:=false)>  _
Partial Public Class Data
    
    Private quoteField As DataQuote
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property Quote() As DataQuote
        Get
            Return Me.quoteField
        End Get
        Set
            Me.quoteField = value
        End Set
    End Property
End Class

'''<remarks/>
<System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),  _
 System.SerializableAttribute(),  _
 System.Diagnostics.DebuggerStepThroughAttribute(),  _
 System.ComponentModel.DesignerCategoryAttribute("code"),  _
 System.Xml.Serialization.XmlTypeAttribute(AnonymousType:=true)>  _
Partial Public Class DataQuote
    
    Private productField() As DataQuoteProduct
    
    Private idField As Integer
    
    Private idFieldSpecified As Boolean
    
    Private nameField As String
    
    Private createdByField As String
    
    Private creatorEmailField As String
    
    Private requestorCompanyField As String
    
    Private requestorNameField As String
    
    Private requestorEmailField As String
    
    Private supplierField As String
    
    Private uRLProductImageField As String
    
    Private uRLProductSpecsField As String
    
    Private notesField As String
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute("Product", Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property Product() As DataQuoteProduct()
        Get
            Return Me.productField
        End Get
        Set
            Me.productField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property ID() As Integer
        Get
            Return Me.idField
        End Get
        Set
            Me.idField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlIgnoreAttribute()>  _
    Public Property IDSpecified() As Boolean
        Get
            Return Me.idFieldSpecified
        End Get
        Set
            Me.idFieldSpecified = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property Name() As String
        Get
            Return Me.nameField
        End Get
        Set
            Me.nameField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property CreatedBy() As String
        Get
            Return Me.createdByField
        End Get
        Set
            Me.createdByField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property CreatorEmail() As String
        Get
            Return Me.creatorEmailField
        End Get
        Set
            Me.creatorEmailField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property RequestorCompany() As String
        Get
            Return Me.requestorCompanyField
        End Get
        Set
            Me.requestorCompanyField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property RequestorName() As String
        Get
            Return Me.requestorNameField
        End Get
        Set
            Me.requestorNameField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property RequestorEmail() As String
        Get
            Return Me.requestorEmailField
        End Get
        Set
            Me.requestorEmailField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property Supplier() As String
        Get
            Return Me.supplierField
        End Get
        Set
            Me.supplierField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute(DataType:="anyURI")>  _
    Public Property URLProductImage() As String
        Get
            Return Me.uRLProductImageField
        End Get
        Set
            Me.uRLProductImageField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute(DataType:="anyURI")>  _
    Public Property URLProductSpecs() As String
        Get
            Return Me.uRLProductSpecsField
        End Get
        Set
            Me.uRLProductSpecsField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlAttributeAttribute()>  _
    Public Property Notes() As String
        Get
            Return Me.notesField
        End Get
        Set
            Me.notesField = value
        End Set
    End Property
End Class

'''<remarks/>
<System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),  _
 System.SerializableAttribute(),  _
 System.Diagnostics.DebuggerStepThroughAttribute(),  _
 System.ComponentModel.DesignerCategoryAttribute("code"),  _
 System.Xml.Serialization.XmlTypeAttribute(AnonymousType:=true)>  _
Partial Public Class DataQuoteProduct
    
    Private classField As String
    
    Private partNumField As String
    
    Private supplierPartNumField As String
    
    Private descriptionField As String
    
    Private qtyField As String
    
    Private priceField As Decimal
    
    Private priceFieldSpecified As Boolean
    
    Private listPriceField As Decimal
    
    Private listPriceFieldSpecified As Boolean
    
    Private oPGrefField As String
    
    Private rebateValueField As Decimal
    
    Private rebateValueFieldSpecified As Boolean
    
    Private uRLProductImageField As String
    
    Private uRLProductSpecsField As String
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property [Class]() As String
        Get
            Return Me.classField
        End Get
        Set
            Me.classField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property PartNum() As String
        Get
            Return Me.partNumField
        End Get
        Set
            Me.partNumField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property SupplierPartNum() As String
        Get
            Return Me.supplierPartNumField
        End Get
        Set
            Me.supplierPartNumField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property Description() As String
        Get
            Return Me.descriptionField
        End Get
        Set
            Me.descriptionField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType:="integer")>  _
    Public Property Qty() As String
        Get
            Return Me.qtyField
        End Get
        Set
            Me.qtyField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property Price() As Decimal
        Get
            Return Me.priceField
        End Get
        Set
            Me.priceField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlIgnoreAttribute()>  _
    Public Property PriceSpecified() As Boolean
        Get
            Return Me.priceFieldSpecified
        End Get
        Set
            Me.priceFieldSpecified = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property ListPrice() As Decimal
        Get
            Return Me.listPriceField
        End Get
        Set
            Me.listPriceField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlIgnoreAttribute()>  _
    Public Property ListPriceSpecified() As Boolean
        Get
            Return Me.listPriceFieldSpecified
        End Get
        Set
            Me.listPriceFieldSpecified = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType:="integer")>  _
    Public Property OPGref() As String
        Get
            Return Me.oPGrefField
        End Get
        Set
            Me.oPGrefField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified)>  _
    Public Property RebateValue() As Decimal
        Get
            Return Me.rebateValueField
        End Get
        Set
            Me.rebateValueField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlIgnoreAttribute()>  _
    Public Property RebateValueSpecified() As Boolean
        Get
            Return Me.rebateValueFieldSpecified
        End Get
        Set
            Me.rebateValueFieldSpecified = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType:="anyURI")>  _
    Public Property URLProductImage() As String
        Get
            Return Me.uRLProductImageField
        End Get
        Set
            Me.uRLProductImageField = value
        End Set
    End Property
    
    '''<remarks/>
    <System.Xml.Serialization.XmlElementAttribute(Form:=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType:="anyURI")>  _
    Public Property URLProductSpecs() As String
        Get
            Return Me.uRLProductSpecsField
        End Get
        Set
            Me.uRLProductSpecsField = value
        End Set
    End Property
End Class
