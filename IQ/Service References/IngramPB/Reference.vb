﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.18444
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.Serialization

Namespace IngramPB
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0"),  _
     System.Runtime.Serialization.DataContractAttribute(Name:="clsLog.Node", [Namespace]:="http://schemas.datacontract.org/2004/07/treeview"),  _
     System.SerializableAttribute()>  _
    Partial Public Class clsLogNode
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private IDField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private childrenField As IngramPB.QueueOfint
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private messageField As String
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private parentField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private severityField As Integer
        
        <System.Runtime.Serialization.OptionalFieldAttribute()>  _
        Private timestampField As Date
        
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
        Public Property ID() As Integer
            Get
                Return Me.IDField
            End Get
            Set
                If (Me.IDField.Equals(value) <> true) Then
                    Me.IDField = value
                    Me.RaisePropertyChanged("ID")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property children() As IngramPB.QueueOfint
            Get
                Return Me.childrenField
            End Get
            Set
                If (Object.ReferenceEquals(Me.childrenField, value) <> true) Then
                    Me.childrenField = value
                    Me.RaisePropertyChanged("children")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property message() As String
            Get
                Return Me.messageField
            End Get
            Set
                If (Object.ReferenceEquals(Me.messageField, value) <> true) Then
                    Me.messageField = value
                    Me.RaisePropertyChanged("message")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property parent() As Integer
            Get
                Return Me.parentField
            End Get
            Set
                If (Me.parentField.Equals(value) <> true) Then
                    Me.parentField = value
                    Me.RaisePropertyChanged("parent")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property severity() As Integer
            Get
                Return Me.severityField
            End Get
            Set
                If (Me.severityField.Equals(value) <> true) Then
                    Me.severityField = value
                    Me.RaisePropertyChanged("severity")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute()>  _
        Public Property timestamp() As Date
            Get
                Return Me.timestampField
            End Get
            Set
                If (Me.timestampField.Equals(value) <> true) Then
                    Me.timestampField = value
                    Me.RaisePropertyChanged("timestamp")
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
     System.Runtime.Serialization.DataContractAttribute(Name:="QueueOfint", [Namespace]:="http://schemas.datacontract.org/2004/07/System.Collections.Generic"),  _
     System.SerializableAttribute()>  _
    Partial Public Class QueueOfint
        Inherits Object
        Implements System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
        
        <System.NonSerializedAttribute()>  _
        Private extensionDataField As System.Runtime.Serialization.ExtensionDataObject
        
        Private _arrayField() As Integer
        
        Private _headField As Integer
        
        Private _sizeField As Integer
        
        Private _tailField As Integer
        
        Private _versionField As Integer
        
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
        Public Property _array() As Integer()
            Get
                Return Me._arrayField
            End Get
            Set
                If (Object.ReferenceEquals(Me._arrayField, value) <> true) Then
                    Me._arrayField = value
                    Me.RaisePropertyChanged("_array")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property _head() As Integer
            Get
                Return Me._headField
            End Get
            Set
                If (Me._headField.Equals(value) <> true) Then
                    Me._headField = value
                    Me.RaisePropertyChanged("_head")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property _size() As Integer
            Get
                Return Me._sizeField
            End Get
            Set
                If (Me._sizeField.Equals(value) <> true) Then
                    Me._sizeField = value
                    Me.RaisePropertyChanged("_size")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property _tail() As Integer
            Get
                Return Me._tailField
            End Get
            Set
                If (Me._tailField.Equals(value) <> true) Then
                    Me._tailField = value
                    Me.RaisePropertyChanged("_tail")
                End If
            End Set
        End Property
        
        <System.Runtime.Serialization.DataMemberAttribute(IsRequired:=true)>  _
        Public Property _version() As Integer
            Get
                Return Me._versionField
            End Get
            Set
                If (Me._versionField.Equals(value) <> true) Then
                    Me._versionField = value
                    Me.RaisePropertyChanged("_version")
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
     System.ServiceModel.ServiceContractAttribute(ConfigurationName:="IngramPB.I_Logging")>  _
    Public Interface I_Logging
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/GetLogNode", ReplyAction:="http://tempuri.org/I_Logging/GetLogNodeResponse")>  _
        Function GetLogNode(ByVal nodeID As Integer) As IngramPB.clsLogNode
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/GetLogNode", ReplyAction:="http://tempuri.org/I_Logging/GetLogNodeResponse")>  _
        Function GetLogNodeAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/GetNodeChildren", ReplyAction:="http://tempuri.org/I_Logging/GetNodeChildrenResponse")>  _
        Function GetNodeChildren(ByVal nodeID As Integer) As IngramPB.clsLogNode()
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/GetNodeChildren", ReplyAction:="http://tempuri.org/I_Logging/GetNodeChildrenResponse")>  _
        Function GetNodeChildrenAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode())
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/find", ReplyAction:="http://tempuri.org/I_Logging/findResponse")>  _
        Function find(ByVal nodeID As Integer, ByVal text As String) As IngramPB.clsLogNode()
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/find", ReplyAction:="http://tempuri.org/I_Logging/findResponse")>  _
        Function findAsync(ByVal nodeID As Integer, ByVal text As String) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode())
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/Ancestors", ReplyAction:="http://tempuri.org/I_Logging/AncestorsResponse")>  _
        Function Ancestors(ByVal nodeID As Integer) As Integer()
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/Ancestors", ReplyAction:="http://tempuri.org/I_Logging/AncestorsResponse")>  _
        Function AncestorsAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of Integer())
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/Prune", ReplyAction:="http://tempuri.org/I_Logging/PruneResponse")>  _
        Function Prune(ByVal nodeID As Integer, ByVal slash As Boolean) As Integer
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/Prune", ReplyAction:="http://tempuri.org/I_Logging/PruneResponse")>  _
        Function PruneAsync(ByVal nodeID As Integer, ByVal slash As Boolean) As System.Threading.Tasks.Task(Of Integer)
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/IngramPriceBand", ReplyAction:="http://tempuri.org/I_Logging/IngramPriceBandResponse")>  _
        Function IngramPriceBand(ByVal hostid As String, ByVal customerNumber As String) As Integer
        
        <System.ServiceModel.OperationContractAttribute(Action:="http://tempuri.org/I_Logging/IngramPriceBand", ReplyAction:="http://tempuri.org/I_Logging/IngramPriceBandResponse")>  _
        Function IngramPriceBandAsync(ByVal hostid As String, ByVal customerNumber As String) As System.Threading.Tasks.Task(Of Integer)
    End Interface
    
    <System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Public Interface I_LoggingChannel
        Inherits IngramPB.I_Logging, System.ServiceModel.IClientChannel
    End Interface
    
    <System.Diagnostics.DebuggerStepThroughAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")>  _
    Partial Public Class I_LoggingClient
        Inherits System.ServiceModel.ClientBase(Of IngramPB.I_Logging)
        Implements IngramPB.I_Logging
        
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
        
        Public Function GetLogNode(ByVal nodeID As Integer) As IngramPB.clsLogNode Implements IngramPB.I_Logging.GetLogNode
            Return MyBase.Channel.GetLogNode(nodeID)
        End Function
        
        Public Function GetLogNodeAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode) Implements IngramPB.I_Logging.GetLogNodeAsync
            Return MyBase.Channel.GetLogNodeAsync(nodeID)
        End Function
        
        Public Function GetNodeChildren(ByVal nodeID As Integer) As IngramPB.clsLogNode() Implements IngramPB.I_Logging.GetNodeChildren
            Return MyBase.Channel.GetNodeChildren(nodeID)
        End Function
        
        Public Function GetNodeChildrenAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode()) Implements IngramPB.I_Logging.GetNodeChildrenAsync
            Return MyBase.Channel.GetNodeChildrenAsync(nodeID)
        End Function
        
        Public Function find(ByVal nodeID As Integer, ByVal text As String) As IngramPB.clsLogNode() Implements IngramPB.I_Logging.find
            Return MyBase.Channel.find(nodeID, text)
        End Function
        
        Public Function findAsync(ByVal nodeID As Integer, ByVal text As String) As System.Threading.Tasks.Task(Of IngramPB.clsLogNode()) Implements IngramPB.I_Logging.findAsync
            Return MyBase.Channel.findAsync(nodeID, text)
        End Function
        
        Public Function Ancestors(ByVal nodeID As Integer) As Integer() Implements IngramPB.I_Logging.Ancestors
            Return MyBase.Channel.Ancestors(nodeID)
        End Function
        
        Public Function AncestorsAsync(ByVal nodeID As Integer) As System.Threading.Tasks.Task(Of Integer()) Implements IngramPB.I_Logging.AncestorsAsync
            Return MyBase.Channel.AncestorsAsync(nodeID)
        End Function
        
        Public Function Prune(ByVal nodeID As Integer, ByVal slash As Boolean) As Integer Implements IngramPB.I_Logging.Prune
            Return MyBase.Channel.Prune(nodeID, slash)
        End Function
        
        Public Function PruneAsync(ByVal nodeID As Integer, ByVal slash As Boolean) As System.Threading.Tasks.Task(Of Integer) Implements IngramPB.I_Logging.PruneAsync
            Return MyBase.Channel.PruneAsync(nodeID, slash)
        End Function
        
        Public Function IngramPriceBand(ByVal hostid As String, ByVal customerNumber As String) As Integer Implements IngramPB.I_Logging.IngramPriceBand
            Return MyBase.Channel.IngramPriceBand(hostid, customerNumber)
        End Function
        
        Public Function IngramPriceBandAsync(ByVal hostid As String, ByVal customerNumber As String) As System.Threading.Tasks.Task(Of Integer) Implements IngramPB.I_Logging.IngramPriceBandAsync
            Return MyBase.Channel.IngramPriceBandAsync(hostid, customerNumber)
        End Function
    End Class
End Namespace
