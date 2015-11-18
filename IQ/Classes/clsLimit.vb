Public Class clsLimit

    '[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref]
    Property Qinstalled As Integer
    Property Qmin As Integer
    Property Qmax As Integer
    Property MinIncr As Integer
    Property PrefIncr As Integer

    Public Sub New(Installed As Integer, Qmin As Integer, Qmax As Integer, MinIncr As Integer, PrefIncr As Integer)

        Me.Qinstalled = Installed
        Me.Qmin = Qmin
        Me.Qmax = Qmax
        Me.MinIncr = MinIncr
        Me.PrefIncr = PrefIncr

    End Sub

End Class
