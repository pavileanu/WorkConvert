Imports System.IO

Module Profiling

    Public Profile As Dictionary(Of String, clsProfile)

    Public Sub Pmark(key As String)

        Exit Sub

        If Profile Is Nothing Then
            Profile = New Dictionary(Of String, clsProfile)
        End If

        If Not Profile.ContainsKey(key) Then
            Profile.Add(key, New clsProfile)
        End If

        Profile(key).PMark()

    End Sub

    Public Sub Pacc(key$)

        Exit Sub

        Profile(key$).PAccumulate()

    End Sub






End Module
