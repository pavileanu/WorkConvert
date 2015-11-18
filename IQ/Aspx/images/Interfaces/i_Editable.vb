
''' <summary>
''' An interface is a defined set of Methods (Subs and functions) that an IMPLEMENTING class *must* expose
''' </summary>
''' <remarks>Classes that want to use the generic editor shoudl IMPLIMENT this interface - which will ensure they have the appropriate INSERT,UPDATE,DELETE and DisplayName methods</remarks>
Public Interface i_Editable  'An interface defines a set of function that a class (which IMPLEMENTS it) *must* expose - It has no purpose at runtime - it's really a compile time syntax/sanity checking mechanism

    'Sub New() - All editbale objects must have a parameterless constructor - for some obscure reason this can't be defined in an interface

    Function displayName(Language As clsLanguage) As String
    Function Insert(ByRef Errormessages As List(Of String)) As Object
    Sub update(ByRef Errormessages As List(Of String))
    Sub delete(ByRef Errormessages As List(Of String))

End Interface