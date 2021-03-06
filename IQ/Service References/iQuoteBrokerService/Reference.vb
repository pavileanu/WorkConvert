﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.34014
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.Serialization

Namespace iQuoteBrokerService
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="Node", [Namespace]:="http://schemas.datacontract.org/2004/07/iQuoteBroker.Classes"),  _
     System.SerializableAttribute()>  _
    Partial Public Class Node
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private IdField As System.Guid
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private IsBrokerField As Boolean
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private LastConnectedField As Date
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private NameField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private StateStringField As String
        
        <Global.System.ComponentModel.BrowsableAttribute(false)>  _
        Public Property ExtensionData() As System.Runtime.Serialization.ExtensionDataObject Implements System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
            Get
                Return Me.extensionDataField
            End Get
            Set
                Me.extensionDataField = value
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property Id() As System.Guid
            Get
                Return Me.IdField
            End Get
            Set
                If (Me.IdField.Equals(value) <> true) Then
                    Me.IdField = value
                    Me.RaisePropertyChanged("Id")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property IsBroker() As Boolean
            Get
                Return Me.IsBrokerField
            End Get
            Set
                If (Me.IsBrokerField.Equals(value) <> true) Then
                    Me.IsBrokerField = value
                    Me.RaisePropertyChanged("IsBroker")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property LastConnected() As Date
            Get
                Return Me.LastConnectedField
            End Get
            Set
                If (Me.LastConnectedField.Equals(value) <> true) Then
                    Me.LastConnectedField = value
                    Me.RaisePropertyChanged("LastConnected")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property Name() As String
            Get
                Return Me.NameField
            End Get
            Set
                If (Object.ReferenceEquals(Me.NameField, value) <> true) Then
                    Me.NameField = value
                    Me.RaisePropertyChanged("Name")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property StateString() As String
            Get
                Return Me.StateStringField
            End Get
            Set
                If (Object.ReferenceEquals(Me.StateStringField, value) <> true) Then
                    Me.StateStringField = value
                    Me.RaisePropertyChanged("StateString")
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
    
    <System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0"),  _
     System.ServiceModel.ServiceContractAttribute(ConfigurationName:="iQuoteBrokerService.IiQuoteBrokerService")>  _
    Public Interface IiQuoteBrokerService
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/UpdateObject", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/UpdateObjectResponse")>  _
        Function UpdateObject(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal TimeStamp As Date) As Boolean
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/UpdateObject", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/UpdateObjectResponse")>  _
        Function UpdateObjectAsync(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal TimeStamp As Date) As System.Threading.Tasks.Task(Of Boolean)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/RegisterParticipant", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/RegisterParticipantResponse")>  _
        Sub RegisterParticipant(ByVal Id As System.Guid, ByVal Name As String, ByVal URI As String, ByVal IsBroker As Boolean, ByVal IsRootNode As Boolean)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/RegisterParticipant", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/RegisterParticipantResponse")>  _
        Function RegisterParticipantAsync(ByVal Id As System.Guid, ByVal Name As String, ByVal URI As String, ByVal IsBroker As Boolean, ByVal IsRootNode As Boolean) As System.Threading.Tasks.Task
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/ObjectUpdated", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/ObjectUpdatedResponse")>  _
        Function ObjectUpdated(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal UpdateTime As Date) As Boolean
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/ObjectUpdated", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/ObjectUpdatedResponse")>  _
        Function ObjectUpdatedAsync(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal UpdateTime As Date) As System.Threading.Tasks.Task(Of Boolean)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/GetNodes", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/GetNodesResponse")>  _
        Function GetNodes() As System.Collections.Generic.List(Of iQuoteBrokerService.Node)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/IiQuoteBrokerService/GetNodes", ReplyAction:="http://tempuri.org/IiQuoteBrokerService/GetNodesResponse")>  _
        Function GetNodesAsync() As System.Threading.Tasks.Task(Of System.Collections.Generic.List(Of iQuoteBrokerService.Node))
    End Interface
    
    <System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Public Interface IiQuoteBrokerServiceChannel
        Inherits iQuoteBrokerService.IiQuoteBrokerService, System.ServiceModel.IClientChannel
    End Interface
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Partial Public Class IiQuoteBrokerServiceClient
        Inherits System.ServiceModel.ClientBase(Of iQuoteBrokerService.IiQuoteBrokerService)
        Implements iQuoteBrokerService.IiQuoteBrokerService
        
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
        
        Public Function UpdateObject(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal TimeStamp As Date) As Boolean Implements iQuoteBrokerService.IiQuoteBrokerService.UpdateObject
            Return MyBase.Channel.UpdateObject(Id, Path, Properties, TimeStamp)
        End Function
        
        Public Function UpdateObjectAsync(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal TimeStamp As Date) As System.Threading.Tasks.Task(Of Boolean) Implements iQuoteBrokerService.IiQuoteBrokerService.UpdateObjectAsync
            Return MyBase.Channel.UpdateObjectAsync(Id, Path, Properties, TimeStamp)
        End Function
        
        Public Sub RegisterParticipant(ByVal Id As System.Guid, ByVal Name As String, ByVal URI As String, ByVal IsBroker As Boolean, ByVal IsRootNode As Boolean) Implements iQuoteBrokerService.IiQuoteBrokerService.RegisterParticipant
            MyBase.Channel.RegisterParticipant(Id, Name, URI, IsBroker, IsRootNode)
        End Sub
        
        Public Function RegisterParticipantAsync(ByVal Id As System.Guid, ByVal Name As String, ByVal URI As String, ByVal IsBroker As Boolean, ByVal IsRootNode As Boolean) As System.Threading.Tasks.Task Implements iQuoteBrokerService.IiQuoteBrokerService.RegisterParticipantAsync
            Return MyBase.Channel.RegisterParticipantAsync(Id, Name, URI, IsBroker, IsRootNode)
        End Function
        
        Public Function ObjectUpdated(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal UpdateTime As Date) As Boolean Implements iQuoteBrokerService.IiQuoteBrokerService.ObjectUpdated
            Return MyBase.Channel.ObjectUpdated(Id, Path, Properties, UpdateTime)
        End Function
        
        Public Function ObjectUpdatedAsync(ByVal Id As System.Guid, ByVal Path As String, ByVal Properties As System.Collections.Generic.List(Of System.Tuple(Of String, String, Object, System.Nullable(Of Integer))), ByVal UpdateTime As Date) As System.Threading.Tasks.Task(Of Boolean) Implements iQuoteBrokerService.IiQuoteBrokerService.ObjectUpdatedAsync
            Return MyBase.Channel.ObjectUpdatedAsync(Id, Path, Properties, UpdateTime)
        End Function
        
        Public Function GetNodes() As System.Collections.Generic.List(Of iQuoteBrokerService.Node) Implements iQuoteBrokerService.IiQuoteBrokerService.GetNodes
            Return MyBase.Channel.GetNodes
        End Function
        
        Public Function GetNodesAsync() As System.Threading.Tasks.Task(Of System.Collections.Generic.List(Of iQuoteBrokerService.Node)) Implements iQuoteBrokerService.IiQuoteBrokerService.GetNodesAsync
            Return MyBase.Channel.GetNodesAsync
        End Function
    End Class
End Namespace
