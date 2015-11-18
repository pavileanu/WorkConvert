Public Class clsAccountScreenField
    Public AccountID As Integer
    Public ScreenID As Integer
    Public Path As String
    Public FieldId As Integer
    Public Order As Integer
    Public Width As Integer
    Public Visibility As Boolean
    Public Sort As String
    Public Filter As String
    Public Description As clsTranslation
    Public PromoColumn As Boolean
    Public ReadOnly Property LabelText As String
        Get
            Return Description.text(English)
        End Get
    End Property


    Private _grownWidth As Single?
    Public Property GrownWidth As Single
        Get
            If _grownWidth.HasValue Then Return _grownWidth.Value Else Return Width
        End Get
        Set(value As Single)
            If value = 0 Then _grownWidth = Nothing Else _grownWidth = value
        End Set
    End Property

    Public ReadOnly Property HasScreenOverride As Boolean
        Get
            Return iq.ScreenOverrides.ToList().Where(Function(a) a.AccountID = AccountID And a.FieldId = FieldId And a.ScreenID = ScreenID And a.Path = Path).Count() > 0
        End Get
    End Property

    Public DisplayUnit As clsUnit
    Public ReadOnly Property DisplayUnitSymbol As String
        Get
            Return If(DisplayUnit Is Nothing, String.Empty, DisplayUnit.Symbol)
        End Get
    End Property



    Public Function ConvertValueToUnit(value As Long, origialUnit As Integer?)
        If DisplayUnit Is Nothing Then Return value
        If origialUnit Is Nothing Then Return value
        If Not iq.Conversions.ContainsKey(origialUnit) Then Return value
        If Not iq.Conversions(origialUnit).ContainsKey(DisplayUnit.ID) Then Return value

        Return value * iq.Conversions(origialUnit)(DisplayUnit.ID)
    End Function

End Class
