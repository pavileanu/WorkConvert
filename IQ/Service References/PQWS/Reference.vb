﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.34209
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.Serialization

Namespace PQWS
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="CPCHierarchyCarePackResults", [Namespace]:="http://schemas.datacontract.org/2004/07/WebServicesPrj.CPCServiceLayer.ValueObjec"& _ 
        "ts"),  _
     System.SerializableAttribute()>  _
    Partial Public Class CPCHierarchyCarePackResults
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private AllHPCarePacksField() As PQWS.CPCCarePack
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private CountryDetailsField As PQWS.CPCCountry
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private RecommendedHPCarePacksField() As PQWS.CPCCarePack
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private SupportRecommendationsField() As PQWS.SupportRecommendation
        
        <Global.System.ComponentModel.BrowsableAttribute(false)>  _
        Public Property ExtensionData() As System.Runtime.Serialization.ExtensionDataObject Implements System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
            Get
                Return Me.extensionDataField
            End Get
            Set
                Me.extensionDataField = value
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property AllHPCarePacks() As PQWS.CPCCarePack()
            Get
                Return Me.AllHPCarePacksField
            End Get
            Set
                If (Object.ReferenceEquals(Me.AllHPCarePacksField, value) <> true) Then
                    Me.AllHPCarePacksField = value
                    Me.RaisePropertyChanged("AllHPCarePacks")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property CountryDetails() As PQWS.CPCCountry
            Get
                Return Me.CountryDetailsField
            End Get
            Set
                If (Object.ReferenceEquals(Me.CountryDetailsField, value) <> true) Then
                    Me.CountryDetailsField = value
                    Me.RaisePropertyChanged("CountryDetails")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property RecommendedHPCarePacks() As PQWS.CPCCarePack()
            Get
                Return Me.RecommendedHPCarePacksField
            End Get
            Set
                If (Object.ReferenceEquals(Me.RecommendedHPCarePacksField, value) <> true) Then
                    Me.RecommendedHPCarePacksField = value
                    Me.RaisePropertyChanged("RecommendedHPCarePacks")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property SupportRecommendations() As PQWS.SupportRecommendation()
            Get
                Return Me.SupportRecommendationsField
            End Get
            Set
                If (Object.ReferenceEquals(Me.SupportRecommendationsField, value) <> true) Then
                    Me.SupportRecommendationsField = value
                    Me.RaisePropertyChanged("SupportRecommendations")
                End If
            End Set
        End Property
        
        Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        
        Protected Sub RaisePropertyChanged(ByVal propertyName As String)
            Dim propertyChanged As System.ComponentModel.PropertyChangedEventHandler = Me.PropertyChangedEvent
            If (Not (propertyChanged) Is Nothing) Then
                propertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(propertyName))
            End If
        End Sub
    End Class
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="CPCCountry", [Namespace]:="http://schemas.datacontract.org/2004/07/WebServicesPrj.CPCServiceLayer.ValueObjec"& _ 
        "ts"),  _
     System.SerializableAttribute()>  _
    Partial Public Class CPCCountry
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        Private LocalPriceCurrencyCodeField As String
        
        <Global.System.ComponentModel.BrowsableAttribute(false)>  _
        Public Property ExtensionData() As System.Runtime.Serialization.ExtensionDataObject Implements System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
            Get
                Return Me.extensionDataField
            End Get
            Set
                Me.extensionDataField = value
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property LocalPriceCurrencyCode() As String
            Get
                Return Me.LocalPriceCurrencyCodeField
            End Get
            Set
                If (Object.ReferenceEquals(Me.LocalPriceCurrencyCodeField, value) <> true) Then
                    Me.LocalPriceCurrencyCodeField = value
                    Me.RaisePropertyChanged("LocalPriceCurrencyCode")
                End If
            End Set
        End Property
        
        Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        
        Protected Sub RaisePropertyChanged(ByVal propertyName As String)
            Dim propertyChanged As System.ComponentModel.PropertyChangedEventHandler = Me.PropertyChangedEvent
            If (Not (propertyChanged) Is Nothing) Then
                propertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(propertyName))
            End If
        End Sub
    End Class
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="CPCCarePack", [Namespace]:="http://schemas.datacontract.org/2004/07/WebServicesPrj.CPCServiceLayer.ValueObjec"& _ 
        "ts"),  _
     System.SerializableAttribute(),  _
     System.Runtime.Serialization.KnownTypeAttribute(GetType(PQWS.SupportRecommendationCarePack))>  _
    Partial Public Class CPCCarePack
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private CarePackProductNumberField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private OrderOfPreferenceField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private PriceLocalListField As Double
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private ServiceDescriptionField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private ServiceLevelField As Short
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private ServiceLevelGroupcodeField As String
        
        <Global.System.ComponentModel.BrowsableAttribute(false)>  _
        Public Property ExtensionData() As System.Runtime.Serialization.ExtensionDataObject Implements System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
            Get
                Return Me.extensionDataField
            End Get
            Set
                Me.extensionDataField = value
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property CarePackProductNumber() As String
            Get
                Return Me.CarePackProductNumberField
            End Get
            Set
                If (Object.ReferenceEquals(Me.CarePackProductNumberField, value) <> true) Then
                    Me.CarePackProductNumberField = value
                    Me.RaisePropertyChanged("CarePackProductNumber")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property OrderOfPreference() As Integer
            Get
                Return Me.OrderOfPreferenceField
            End Get
            Set
                If (Me.OrderOfPreferenceField.Equals(value) <> true) Then
                    Me.OrderOfPreferenceField = value
                    Me.RaisePropertyChanged("OrderOfPreference")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property PriceLocalList() As Double
            Get
                Return Me.PriceLocalListField
            End Get
            Set
                If (Me.PriceLocalListField.Equals(value) <> true) Then
                    Me.PriceLocalListField = value
                    Me.RaisePropertyChanged("PriceLocalList")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property ServiceDescription() As String
            Get
                Return Me.ServiceDescriptionField
            End Get
            Set
                If (Object.ReferenceEquals(Me.ServiceDescriptionField, value) <> true) Then
                    Me.ServiceDescriptionField = value
                    Me.RaisePropertyChanged("ServiceDescription")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property ServiceLevel() As Short
            Get
                Return Me.ServiceLevelField
            End Get
            Set
                If (Me.ServiceLevelField.Equals(value) <> true) Then
                    Me.ServiceLevelField = value
                    Me.RaisePropertyChanged("ServiceLevel")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property ServiceLevelGroupcode() As String
            Get
                Return Me.ServiceLevelGroupcodeField
            End Get
            Set
                If (Object.ReferenceEquals(Me.ServiceLevelGroupcodeField, value) <> true) Then
                    Me.ServiceLevelGroupcodeField = value
                    Me.RaisePropertyChanged("ServiceLevelGroupcode")
                End If
            End Set
        End Property
        
        Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        
        Protected Sub RaisePropertyChanged(ByVal propertyName As String)
            Dim propertyChanged As System.ComponentModel.PropertyChangedEventHandler = Me.PropertyChangedEvent
            If (Not (propertyChanged) Is Nothing) Then
                propertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(propertyName))
            End If
        End Sub
    End Class
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="SupportRecommendation", [Namespace]:="http://schemas.datacontract.org/2004/07/WebServicesPrj.CPCServiceLayer.ValueObjec"& _ 
        "ts"),  _
     System.SerializableAttribute()>  _
    Partial Public Class SupportRecommendation
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private CodeField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private DescriptionLocalField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private DescriptionWWField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private DisplayOrderField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private DurationYearQtyField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private RecommendLevelCodeField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private RecommendTypeCodeField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private SupportRecommendationPackagesField() As PQWS.SupportRecommendationCarePack
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private TotalValueField As Double
        
        <Global.System.ComponentModel.BrowsableAttribute(false)>  _
        Public Property ExtensionData() As System.Runtime.Serialization.ExtensionDataObject Implements System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
            Get
                Return Me.extensionDataField
            End Get
            Set
                Me.extensionDataField = value
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property Code() As String
            Get
                Return Me.CodeField
            End Get
            Set
                If (Object.ReferenceEquals(Me.CodeField, value) <> true) Then
                    Me.CodeField = value
                    Me.RaisePropertyChanged("Code")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property DescriptionLocal() As String
            Get
                Return Me.DescriptionLocalField
            End Get
            Set
                If (Object.ReferenceEquals(Me.DescriptionLocalField, value) <> true) Then
                    Me.DescriptionLocalField = value
                    Me.RaisePropertyChanged("DescriptionLocal")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property DescriptionWW() As String
            Get
                Return Me.DescriptionWWField
            End Get
            Set
                If (Object.ReferenceEquals(Me.DescriptionWWField, value) <> true) Then
                    Me.DescriptionWWField = value
                    Me.RaisePropertyChanged("DescriptionWW")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property DisplayOrder() As Integer
            Get
                Return Me.DisplayOrderField
            End Get
            Set
                If (Me.DisplayOrderField.Equals(value) <> true) Then
                    Me.DisplayOrderField = value
                    Me.RaisePropertyChanged("DisplayOrder")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property DurationYearQty() As Integer
            Get
                Return Me.DurationYearQtyField
            End Get
            Set
                If (Me.DurationYearQtyField.Equals(value) <> true) Then
                    Me.DurationYearQtyField = value
                    Me.RaisePropertyChanged("DurationYearQty")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property RecommendLevelCode() As String
            Get
                Return Me.RecommendLevelCodeField
            End Get
            Set
                If (Object.ReferenceEquals(Me.RecommendLevelCodeField, value) <> true) Then
                    Me.RecommendLevelCodeField = value
                    Me.RaisePropertyChanged("RecommendLevelCode")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property RecommendTypeCode() As String
            Get
                Return Me.RecommendTypeCodeField
            End Get
            Set
                If (Object.ReferenceEquals(Me.RecommendTypeCodeField, value) <> true) Then
                    Me.RecommendTypeCodeField = value
                    Me.RaisePropertyChanged("RecommendTypeCode")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property SupportRecommendationPackages() As PQWS.SupportRecommendationCarePack()
            Get
                Return Me.SupportRecommendationPackagesField
            End Get
            Set
                If (Object.ReferenceEquals(Me.SupportRecommendationPackagesField, value) <> true) Then
                    Me.SupportRecommendationPackagesField = value
                    Me.RaisePropertyChanged("SupportRecommendationPackages")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue:=false)>  _
        Public Property TotalValue() As Double
            Get
                Return Me.TotalValueField
            End Get
            Set
                If (Me.TotalValueField.Equals(value) <> true) Then
                    Me.TotalValueField = value
                    Me.RaisePropertyChanged("TotalValue")
                End If
            End Set
        End Property
        
        Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        
        Protected Sub RaisePropertyChanged(ByVal propertyName As String)
            Dim propertyChanged As System.ComponentModel.PropertyChangedEventHandler = Me.PropertyChangedEvent
            If (Not (propertyChanged) Is Nothing) Then
                propertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(propertyName))
            End If
        End Sub
    End Class
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="SupportRecommendationCarePack", [Namespace]:="http://schemas.datacontract.org/2004/07/WebServicesPrj.CPCServiceLayer.ValueObjec"& _ 
        "ts"),  _
     System.SerializableAttribute()>  _
    Partial Public Class SupportRecommendationCarePack
        Inherits PQWS.CPCCarePack
        
        Private DisplayOrderField As Integer
        
        Private QuantityField As Integer
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property DisplayOrder() As Integer
            Get
                Return Me.DisplayOrderField
            End Get
            Set
                If (Me.DisplayOrderField.Equals(value) <> true) Then
                    Me.DisplayOrderField = value
                    Me.RaisePropertyChanged("DisplayOrder")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property Quantity() As Integer
            Get
                Return Me.QuantityField
            End Get
            Set
                If (Me.QuantityField.Equals(value) <> true) Then
                    Me.QuantityField = value
                    Me.RaisePropertyChanged("Quantity")
                End If
            End Set
        End Property
    End Class
    
    <System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0"),  _
     System.ServiceModel.ServiceContractAttribute(ConfigurationName:="PQWS.IPQWS")>  _
    Public Interface IPQWS
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IPQWS/Hello", ReplyAction:="http://tempuri.org/IPQWS/HelloResponse")>  _
        Function Hello() As String
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IPQWS/Hello", ReplyAction:="http://tempuri.org/IPQWS/HelloResponse")>  _
        Function HelloAsync() As System.Threading.Tasks.Task(Of String)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IPQWS/HPCarePacks", ReplyAction:="http://tempuri.org/IPQWS/HPCarePacksResponse")>  _
        Function HPCarePacks(ByVal manufacturer As String, ByVal skuCode As String, ByVal countryCode As String, ByVal languageCode As String) As PQWS.CPCHierarchyCarePackResults
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IPQWS/HPCarePacks", ReplyAction:="http://tempuri.org/IPQWS/HPCarePacksResponse")>  _
        Function HPCarePacksAsync(ByVal manufacturer As String, ByVal skuCode As String, ByVal countryCode As String, ByVal languageCode As String) As System.Threading.Tasks.Task(Of PQWS.CPCHierarchyCarePackResults)
    End Interface
    
    <System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Public Interface IPQWSChannel
        Inherits PQWS.IPQWS, System.ServiceModel.IClientChannel
    End Interface
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Partial Public Class PQWSClient
        Inherits System.ServiceModel.ClientBase(Of PQWS.IPQWS)
        Implements PQWS.IPQWS
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal endpointConfigurationName As String)
            MyBase.New(endpointConfigurationName)
        End Sub
        
        Public Sub New(ByVal endpointConfigurationName As String, ByVal remoteAddress As String)
            MyBase.New(endpointConfigurationName, remoteAddress)
        End Sub
        
        Public Sub New(ByVal endpointConfigurationName As String, ByVal remoteAddress As System.ServiceModel.EndpointAddress)
            MyBase.New(endpointConfigurationName, remoteAddress)
        End Sub
        
        Public Sub New(ByVal binding As System.ServiceModel.Channels.Binding, ByVal remoteAddress As System.ServiceModel.EndpointAddress)
            MyBase.New(binding, remoteAddress)
        End Sub
        
        Public Function Hello() As String Implements PQWS.IPQWS.Hello
            Return MyBase.Channel.Hello
        End Function
        
        Public Function HelloAsync() As System.Threading.Tasks.Task(Of String) Implements PQWS.IPQWS.HelloAsync
            Return MyBase.Channel.HelloAsync
        End Function
        
        Public Function HPCarePacks(ByVal manufacturer As String, ByVal skuCode As String, ByVal countryCode As String, ByVal languageCode As String) As PQWS.CPCHierarchyCarePackResults Implements PQWS.IPQWS.HPCarePacks
            Return MyBase.Channel.HPCarePacks(manufacturer, skuCode, countryCode, languageCode)
        End Function
        
        Public Function HPCarePacksAsync(ByVal manufacturer As String, ByVal skuCode As String, ByVal countryCode As String, ByVal languageCode As String) As System.Threading.Tasks.Task(Of PQWS.CPCHierarchyCarePackResults) Implements PQWS.IPQWS.HPCarePacksAsync
            Return MyBase.Channel.HPCarePacksAsync(manufacturer, skuCode, countryCode, languageCode)
        End Function
    End Class
End Namespace
