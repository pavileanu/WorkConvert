
/// <summary>
/// An interface is a defined set of Methods (Subs and functions) that an IMPLEMENTING class *must* expose
/// </summary>
/// <remarks>Classes that want to use the generic editor shoudl IMPLIMENT this interface - which will ensure they have the appropriate INSERT,UPDATE,DELETE and DisplayName methods</remarks>
public interface i_Editable
{
	//An interface defines a set of function that a class (which IMPLEMENTS it) *must* expose - It has no purpose at runtime - it's really a compile time syntax/sanity checking mechanism

	//Sub New() - All editbale objects must have a parameterless constructor - for some obscure reason this can't be defined in an interface

	string displayName(clsLanguage Language);
	object Insert(ref List<string> Errormessages);
	void update(ref List<string> Errormessages);

	void delete(ref List<string> Errormessages);
}
