
Public Class LoggingList(Of T)
    'Inherits List(Of String)
    Private Class clsData
        Public Message As String
        Public HeapSize As Double
        Public Time As Double
    End Class
    Private data As List(Of clsData) = New List(Of clsData)()
    Private sw As Stopwatch = New Stopwatch()
    Private StartBytes As Double

    Public Sub Clear()
        sw.Reset()
        data.Clear()
    End Sub
    Public Sub Start()
        sw.Start()
        StartBytes = System.GC.GetTotalMemory(True)
    End Sub
    Public Sub StopClock()
        sw.stop()
    End Sub
    Public Overloads Function ToList()
        Return data.ToList()
    End Function
    Public Overloads Sub Add(o As String)
        data.Add(New clsData() With {.HeapSize = Math.Round((System.GC.GetTotalMemory(False) - StartBytes) / (1024 ^ 2), 2), .Time = sw.ElapsedMilliseconds / 1000, .Message = o})
    End Sub
End Class