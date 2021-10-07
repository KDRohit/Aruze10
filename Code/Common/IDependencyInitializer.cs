/// <summary>
/// Interface to be implemented by managers that require dependency-based initialization
/// author: Nick Reynolds <nreynolds@zynga.com>
/// </summary>
public interface IDependencyInitializer
{
	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	System.Type[] GetDependencies();		

	// This method should contain the logic required to initialize an object/system.  Once initialization is
	// complete, the implementing class should call the "mgr.InitializationComplete(this)" method to signal
	// that downstream dependencies can be initialized.
	void Initialize(InitializationManager mgr);

	// give a short description of the dependency that we may use for debugging purposes
	string description();
}


