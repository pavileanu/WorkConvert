Public Class CachableDictionary(Of Ta, Tb)
    Inherits Dictionary(Of Ta, Tb)



    Public Overloads Sub Add(key As Ta, value As Tb)
        'Get Dictionary Value
        If value.GetType().Name = "Dictionary`2" Then
            Dim a As Type = value.GetType().GetGenericArguments()(0)
            Dim b As Type = value.GetType().GetGenericArguments()(1)
            Dim typeArgs() As Type = {a, b}
            Dim d As Type = GetType(CachableDictionary(Of ,))
            'value = CTypeDynamic(value, d.MakeGenericType(typeArgs))
            value = Activator.CreateInstance(d.MakeGenericType(typeArgs))
        End If
        If Not value.GetType().Name = "Dictionary`2" Then clsIQ.AuditTrail.Add(DateTime.Now.ToShortTimeString() + " : " + HttpContext.Current.Request("lid") + " : " + key.ToString() + " : " + If(value.GetType().GetProperty("ID") IsNot Nothing, value.GetType().GetProperty("ID").GetValue(value), String.Empty) + " : " + value.ToString())
        MyBase.Add(key, value)
    End Sub

    Default Public Overloads Property Item(ByVal key As Ta) As Tb
        Get
            Return MyBase.Item(key)
        End Get
        Set(value As Tb)
            If value.GetType().Name = "Dictionary`2" Then
                Dim a As Type = value.GetType().GetGenericArguments()(0)
                Dim b As Type = value.GetType().GetGenericArguments()(1)
                Dim typeArgs() As Type = {a, b}
                Dim d As Type = GetType(CachableDictionary(Of ,))
                'value = CTypeDynamic(value, d.MakeGenericType(typeArgs))
                value = Activator.CreateInstance(d.MakeGenericType(typeArgs))
            End If

            If Not value.GetType().Name = "Dictionary`2" Then clsIQ.AuditTrail.Add(DateTime.Now.ToShortTimeString() + " : " + HttpContext.Current.Request("lid") + " : " + key.ToString() + " : " + If(value.GetType().GetProperty("ID") IsNot Nothing, value.GetType().GetProperty("ID").GetValue(value).ToString(), String.Empty) + " : " + value.ToString())
            MyBase.Item(key) = value
        End Set
    End Property





End Class
