<Serializable>
Public Class clsUserState
    Public lid As String
    Public root As String
    Public path As String
    Public QuoteID As Integer?
    Public foci As String 'is a CD list
    Public treeCursorPath As String
    Public branchStates As List(Of KeyValuePair(Of String, clsBranchState))
    Public AgentAccount As Integer
    Public BuyerAccount As Integer
    Public ScreenHeaders As List(Of clsScreenHeaderState)
    Public showOnly As Integer  'Render only this (system) branch (formerly 'configuring')
    Public Paradigm As enumParadigm
    Public mopUpvalues As List(Of KeyValuePair(Of Object, Object))
End Class

Public Class clsScreenHeaderState
    Public Path As String
    Public QuickFiltersVisible As Boolean
    Public Filters As List(Of KeyValuePair(Of Integer, List(Of KeyValuePair(Of clsFilter, List(Of Int64))))) 'As List(Of KeyValuePair(Of clsField, List(Of KeyValuePair(Of clsFilter, List(Of Int64)))))
    Public Sorts As List(Of KeyValuePair(Of Integer, clsPriorityDirection))
End Class