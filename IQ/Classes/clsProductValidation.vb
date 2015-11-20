Imports dataAccess
Public Class clsProductValidation
    Public Property Severity As EnumValidationSeverity
    Public Property Message As clsTranslation
    Public Property CorrectMessage As clsTranslation
    Public Property RequiredOptType As String
    Public Property RequiredQuantity As Integer
    Public Property ValidationType As enumValidationType
    Public Property DependantOptType As String
    Public Property CheckAttribute As String
    Public Property CheckAttributeValue As String
    Public Property DependantCheckAttribute As String
    Public Property DependantCheckAttributeValue As String
    Public Property OptionFamily As String
    Public Property ValidationMessageType As enumValidationMessageType
    Public Property ID As Integer
    Public Property LinkOptType As String
    Public Property LinkTechnology As String
    Public Property LinkOptionFamily As String


    Public Property MessageText As String
        Get
            Return Message.text(English)
        End Get
        Set(value As String)
            Message.text(English) = value
        End Set
    End Property

    Public Property CorrectMessageText As String
        Get
            Return CorrectMessage.text(English)
        End Get
        Set(value As String)
            CorrectMessage.text(English) = value
        End Set
    End Property
    Public Sub New()

    End Sub

    Public Sub New(MessageType As String, OptType As String, ValidationType As String, Seveirty As String, CheckAttribute As String, DependantOptType As String, Message As String, DependantCheckAttribute As String, RequiredQuantity As Integer, DependantCheckAttributeValue As String, CheckAttributeValue As String, OptionFamily As String, SystemType As String, CorrectMessage As String, LinkOptType As String, LinkTechnology As String, LinkOptionFamily As String)
        Me.ValidationMessageType = CType([Enum].Parse(GetType(enumValidationMessageType), MessageType), enumValidationMessageType)
        Me.ValidationType = CType([Enum].Parse(GetType(enumValidationType), ValidationType), enumValidationType)
        Me.Severity = CType([Enum].Parse(GetType(EnumValidationSeverity), Seveirty), EnumValidationSeverity)
        Me.CheckAttribute = CheckAttribute
        Me.DependantOptType = DependantOptType
        Me.RequiredOptType = OptType
        Me.RequiredQuantity = RequiredQuantity
        Me.DependantCheckAttribute = DependantCheckAttribute
        Dim nextKey As Integer
        Me.Message = iq.AddTranslation(Message, English, "", 0, Nothing, nextKey, False)
        Me.DependantCheckAttributeValue = DependantCheckAttributeValue
        Me.CheckAttributeValue = CheckAttributeValue
        Me.OptionFamily = OptionFamily
        Me.CorrectMessage = iq.AddTranslation(CorrectMessage, English, "", 0, Nothing, nextKey, False)
        iq.ProductValidationsAssignment(SystemType).Add(Me)
        Me.LinkOptType = LinkOptType
        Me.LinkTechnology = LinkTechnology
        Me.LinkOptionFamily = LinkOptionFamily

        Dim sql$

        sql$ = "INSERT INTO ProductValidations (ValidationMessageType,OptionFamily,OptType,ValidationType,Severity,CheckAttribute,DependantOptType,RequiredQuantity,DependantCheckAttribute,DependantCheckAttributeValue,CheckAttributeValue,FK_Translation_Key_Message,FK_Translation_Key_CorrectMessage) VALUES (" + da.SqlEncode(Me.ValidationMessageType.ToString) + "," + da.SqlEncode(Me.OptionFamily) + "," + dataAccess.da.SqlEncode(Me.RequiredOptType) + "," + dataAccess.da.SqlEncode(Me.ValidationType.ToString()) + "," + dataAccess.da.SqlEncode(Me.Severity.ToString()) + "," + dataAccess.da.SqlEncode(Me.CheckAttribute) + "," + dataAccess.da.SqlEncode(Me.DependantOptType) + "," + Me.RequiredQuantity.ToString() + "," + da.SqlEncode(Me.DependantCheckAttribute) + "," + da.SqlEncode(Me.DependantCheckAttributeValue) + "," + da.SqlEncode(Me.CheckAttributeValue) + "," + Me.Message.Key.ToString() + "," + Me.CorrectMessage.Key.ToString() + ")"
        Dim d As Integer = dataAccess.da.DBExecutesql(sql, True)
        Me.ID = d

        sql$ = "INSERT INTO ProductValidationMappings VALUES (" + dataAccess.da.SqlEncode(SystemType) + "," + d.ToString() + ")"
        dataAccess.da.DBExecutesql(sql)


    End Sub

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE ProductValidations SET ValidationMessageType=" + da.SqlEncode(Me.ValidationMessageType.ToString) + ", OptionFamily=" + da.SqlEncode(Me.OptionFamily) + ",DependantCheckAttributeValue=" + da.SqlEncode(Me.DependantCheckAttributeValue) + ",CheckAttributeValue=" + da.SqlEncode(Me.CheckAttributeValue) + ",DependantCheckAttribute=" + da.SqlEncode(Me.DependantCheckAttribute) + ",RequiredQuantity=" + Me.RequiredQuantity.ToString() + ",OptType=" + dataAccess.da.SqlEncode(Me.RequiredOptType) + ", ValidationType=" + dataAccess.da.SqlEncode(Me.ValidationType.ToString()) + ", Severity=" + dataAccess.da.SqlEncode(Me.Severity.ToString()) + ",CheckAttribute=" + dataAccess.da.SqlEncode(Me.CheckAttribute) + ", DependantOptType=" + dataAccess.da.SqlEncode(Me.DependantOptType) + ",LinkTechnology=" + dataAccess.da.SqlEncode(Me.LinkTechnology) + ",LinkOptTYpe=" + dataAccess.da.SqlEncode(Me.LinkOptType) + ",LinkOptionFamily=" + dataAccess.da.SqlEncode(Me.LinkOptionFamily) + " WHERE Id=" + Me.ID.ToString()
        dataAccess.da.DBExecutesql(sql)

        sql$ = "UPDATE Translation SET Text=" + da.SqlEncode(Me.MessageText) + " WHERE [Key]=" + Me.Message.Key.ToString()
        dataAccess.da.DBExecutesql(sql)

        If Me.CorrectMessage IsNot Nothing Then
            sql$ = "UPDATE Translation SET Text=" + da.SqlEncode(Me.CorrectMessageText) + " WHERE [Key]=" + Me.CorrectMessage.Key.ToString()
            dataAccess.da.DBExecutesql(sql)
        End If
    End Sub

    Public Sub Delete(sys As String)
        Dim sql$
        sql$ = "DELETE FROM ProductValidationMappings WHERE FK_ProductValidation_Id=" + Me.ID.ToString()
        dataAccess.da.DBExecutesql(sql)

        sql$ = "DELETE FROM ProductValidations WHERE Id=" + Me.ID.ToString()
        dataAccess.da.DBExecutesql(sql)

        iq.ProductValidationsAssignment(sys).RemoveAll(Function(v) v.ID = Me.ID)

    End Sub
End Class

Public Enum enumValidationType
    Slot
    MustHave
    NotToppedUp
    Dependancy
    Mismatch
    CapacityOverload
    MultipleRequred
    UpperWarning
    Exists
    MustHaveProperty
    AtLeastSameQuantity
    Divisible
    SpecRequirement
End Enum