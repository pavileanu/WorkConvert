
Public Class clsSlotSummary

    Public Given As Integer
    Public PreInstalledTaken As Integer
    Public taken As Integer
    Public TotalCapacity As Single  'working variable to total of the quantities of all options taking this slot type (For sumarising HDD and Memory Capacity)
    Public TotalRedundantCapacity As Single 'Put in for PSU's but Paul suggests this will be a common thing for other items...
    Public CapacityUnit As clsUnit

    Public Sub New(Given As Integer, Taken As Integer, totalCapacity As Integer, totalRedundantCapacity As Int32)

        Me.Given = Given
        Me.taken = Taken
        Me.TotalCapacity = totalCapacity
        Me.TotalRedundantCapacity = totalRedundantCapacity
    End Sub


End Class
