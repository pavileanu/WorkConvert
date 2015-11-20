﻿using System.Xml.Serialization;

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------



//
//This source code was auto-generated by xsd, Version=4.0.30319.18020.
//

///<remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),
System.SerializableAttribute(),
System.Diagnostics.DebuggerStepThroughAttribute(),
System.ComponentModel.DesignerCategoryAttribute("code"),
System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true),
System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public
    partial class Data
{

    private DataQuote quoteField;

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public DataQuote Quote
    {
        get
        {
            return this.quoteField;
        }
        set
        {
            this.quoteField = value;
        }
    }
}

///<remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),
System.SerializableAttribute(),
System.Diagnostics.DebuggerStepThroughAttribute(),
System.ComponentModel.DesignerCategoryAttribute("code"),
System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public
    partial class DataQuote
{

    private DataQuoteProduct[] productField;

    private int idField;

    private bool idFieldSpecified;

    private string nameField;

    private string createdByField;

    private string creatorEmailField;

    private string requestorCompanyField;

    private string requestorNameField;

    private string requestorEmailField;

    private string supplierField;

    private string uRLProductImageField;

    private string uRLProductSpecsField;

    private string notesField;

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Product", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public DataQuoteProduct[] Product
    {
        get
        {
            return this.productField;
        }
        set
        {
            this.productField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int ID
    {
        get
        {
            return this.idField;
        }
        set
        {
            this.idField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool IDSpecified
    {
        get
        {
            return this.idFieldSpecified;
        }
        set
        {
            this.idFieldSpecified = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string CreatedBy
    {
        get
        {
            return this.createdByField;
        }
        set
        {
            this.createdByField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string CreatorEmail
    {
        get
        {
            return this.creatorEmailField;
        }
        set
        {
            this.creatorEmailField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string RequestorCompany
    {
        get
        {
            return this.requestorCompanyField;
        }
        set
        {
            this.requestorCompanyField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string RequestorName
    {
        get
        {
            return this.requestorNameField;
        }
        set
        {
            this.requestorNameField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string RequestorEmail
    {
        get
        {
            return this.requestorEmailField;
        }
        set
        {
            this.requestorEmailField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Supplier
    {
        get
        {
            return this.supplierField;
        }
        set
        {
            this.supplierField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string URLProductImage
    {
        get
        {
            return this.uRLProductImageField;
        }
        set
        {
            this.uRLProductImageField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
    public string URLProductSpecs
    {
        get
        {
            return this.uRLProductSpecsField;
        }
        set
        {
            this.uRLProductSpecsField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Notes
    {
        get
        {
            return this.notesField;
        }
        set
        {
            this.notesField = value;
        }
    }
}

///<remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020"),
System.SerializableAttribute(),
System.Diagnostics.DebuggerStepThroughAttribute(),
System.ComponentModel.DesignerCategoryAttribute("code"),
System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public
    partial class DataQuoteProduct
{

    private string classField;

    private string partNumField;

    private string supplierPartNumField;

    private string descriptionField;

    private string qtyField;

    private decimal priceField;

    private bool priceFieldSpecified;

    private decimal listPriceField;

    private bool listPriceFieldSpecified;

    private string oPGrefField;

    private decimal rebateValueField;

    private bool rebateValueFieldSpecified;

    private string uRLProductImageField;

    private string uRLProductSpecsField;

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string Class
    {
        get
        {
            return this.classField;
        }
        set
        {
            this.classField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string PartNum
    {
        get
        {
            return this.partNumField;
        }
        set
        {
            this.partNumField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string SupplierPartNum
    {
        get
        {
            return this.supplierPartNumField;
        }
        set
        {
            this.supplierPartNumField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string Description
    {
        get
        {
            return this.descriptionField;
        }
        set
        {
            this.descriptionField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
    public string Qty
    {
        get
        {
            return this.qtyField;
        }
        set
        {
            this.qtyField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public decimal Price
    {
        get
        {
            return this.priceField;
        }
        set
        {
            this.priceField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool PriceSpecified
    {
        get
        {
            return this.priceFieldSpecified;
        }
        set
        {
            this.priceFieldSpecified = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public decimal ListPrice
    {
        get
        {
            return this.listPriceField;
        }
        set
        {
            this.listPriceField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool ListPriceSpecified
    {
        get
        {
            return this.listPriceFieldSpecified;
        }
        set
        {
            this.listPriceFieldSpecified = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
    public string OPGref
    {
        get
        {
            return this.oPGrefField;
        }
        set
        {
            this.oPGrefField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public decimal RebateValue
    {
        get
        {
            return this.rebateValueField;
        }
        set
        {
            this.rebateValueField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool RebateValueSpecified
    {
        get
        {
            return this.rebateValueFieldSpecified;
        }
        set
        {
            this.rebateValueFieldSpecified = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "anyURI")]
    public string URLProductImage
    {
        get
        {
            return this.uRLProductImageField;
        }
        set
        {
            this.uRLProductImageField = value;
        }
    }

    ///<remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "anyURI")]
    public string URLProductSpecs
    {
        get
        {
            return this.uRLProductSpecsField;
        }
        set
        {
            this.uRLProductSpecsField = value;
        }
    }
}