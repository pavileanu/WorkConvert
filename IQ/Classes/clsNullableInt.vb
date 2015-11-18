Public Class NullableInt

    'This class gives a strongly typed Nullable integer - used for many of the foriegn key pointers
    'you must NEVER test is using IsDbNUll(.. - this would always return false
    'always check if its sqlvalue (suitable for INSERT etc) = "null"

    Public value As Object

    Public ReadOnly Property Displayvalue() As String
        Get
            If Me.value Is DBNull.Value Then Return "" Else Return Me.value
        End Get
    End Property

    Public Sub New()
        Me.value = DBNull.Value

    End Sub
    Public Sub New(value As Object)

        If value Is Nothing Then Stop ' please pass dbnull.value to create a null - or use the paramaterless constructor e.g. New NullableInt()

        If IsDBNull(value) Then
            Me.value = DBNull.Value
        ElseIf TypeOf (value) Is Integer Then
            Me.value = value
        Else
            Stop
        End If

    End Sub

    Public Function sqlvalue() As String

        If IsDBNull(Me.value) Then
            Return "null"
        Else
            Return Me.value.ToString
        End If

    End Function

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim v As NullableInt = CType(obj, NullableInt)
        Return value Is Nothing And v.value Is Nothing OrElse value Is v.value
    End Function


End Class
