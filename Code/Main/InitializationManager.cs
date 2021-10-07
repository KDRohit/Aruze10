using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Initialization manager.  This class is in charge of managing dependencies within an app.  Dependencies can be 
/// added either by calling the Start() method, which collects all managers and deals with their dependencies automatically.
/// It also allows individual dependencies to be added via the AddDependency() method.  When we're ready to start initializing,
/// the StartInitialization() method should be called.  It will ensure that everything is initialized in dependency order.
/// author: Nick Reynolds <nreynolds@zynga.com>
/// </summary>
public class InitializationManager : IResetGame 
{
	/// <summary>
	/// This class is effectively a struct that stores the current initialization stat for a single dependency
	/// </summary>
	protected class InitializationState 
	{
		public bool started;
		public bool initialized;
		public float startTime;
		public float endTime;
	}

	// Stores the pointer to the SVInitializationManager instance
	protected static InitializationManager _instance = null;
	
	// Stores the mapping between a dependency initializer and its current state
	protected static Dictionary<IDependencyInitializer, InitializationState> _dependencies = new Dictionary<IDependencyInitializer, InitializationState>();

	// Stores the mapping of types to dependencies
	protected static Dictionary<Type, IDependencyInitializer> _typesToDependencies = new Dictionary<Type, IDependencyInitializer>();

	// list of dependency descriptions in the order that they were initialized
	public List<string> dependencyOrder = new List<string>();

	// track which round of initialization we are in to help identify when things can be initialized (for debugging)
	private int initializationRound = 0; 
	
	/// <summary>
	/// This static method returns the singleton SVInitializationManager instance
	/// </summary>
	/// <value>
	/// The SVInitializationManager
	/// </value>
	public static InitializationManager Instance 
	{
		get 
		{
			if (_instance == null) 
			{
				_instance = new InitializationManager();	
			}
			return _instance;
		}
	}
	
 	/// <summary>
	/// Class constructor.  This is empty to ensure that it can't be instantiated outside of the Instance() static method
	/// but a test fixture can be inherited from this class (and be instantiated).
	/// </summary>
	protected InitializationManager() 
	{
	}
    
	/// <summary>
	/// This method takes a class that implements the IDependencyInitializer interface, adds it to the list
    /// of dependencies to be tracked, and stores a mapping of the class type to the class itself for later
    /// use in identifying its dependencies.
    /// 
    /// If two instances of the same class are added, the mapping of type->dependency will not be modified
    /// after the first dependency is added.
	/// </summary>
	/// <param name='dependency'>
	/// Instance of a class that implements the IDependencyInitializer interface.
	/// </param>
	public void AddDependency(IDependencyInitializer dependency) 
	{
		// Ensure we don't double-add the same dependency
		if (!_dependencies.ContainsKey(dependency)) 
		{
			// Check to make sure no cycles would be added by this dependency
			if (DoAnyTypesDependOnType(dependency.GetDependencies(), dependency.GetType())) 
			{
				string message = "Cannot add dependency " + dependency.ToString() + " because it would introduce a cycle to the dependency graph";
				Debug.LogError(message);
			}
			
			_dependencies[dependency] = new InitializationState();
		}
		
		// Now make sure we haven't already have a dependency of this type to the _typesToDependencies map
		if (!_typesToDependencies.ContainsKey(dependency.GetType())) 
		{
			_typesToDependencies[dependency.GetType()] = dependency;
		} else 
		{
			Debug.LogError("A dependency of type " + dependency.GetType() + " has already been added to the dependency graph.  A second dependency of this type cannot be added.");
		}
	}

	/// <summary>
	/// Starts the initialization of all known dependencies.
	/// </summary>
	public void StartInitialization() 
	{
#if UNITY_EDITOR
		// This function has been refactored to be GC & memory friendly
		Debug.Log( getAllDependenciesAsText() );
#endif

		InitializeUnblockedDependencies();
	}
	
	/// <summary>
	/// This is the method that should be called from within the IDependencyInitializer::Initialize() 
	/// method to signal to the SVInitializationManager that a dependency has completed its init tasks.
	/// </summary>
	/// <param name='dependency'>
	/// Instance of the class that has completed its init
	/// </param>
	public void InitializationComplete(IDependencyInitializer dependency)  
	{
		
		// Start by ensuring that the passed dependency is actually in our dependency graph
		if (_dependencies.ContainsKey(dependency)) 
		{
			
			// Set the state on the dependency to "initialized"
			_dependencies[dependency].initialized = true;
			_dependencies[dependency].endTime = Time.realtimeSinceStartup;
			float elapsedTime = _dependencies[dependency].endTime - _dependencies[dependency].startTime;
			//czablocki - 2/3/2021 Commenting this out because it spams logs that crash v4.8.4 of Bugsnag when its
			//Notify() hook handles them to leave a Breadcrumb
			//Debug.Log ("=== Initialization Complete: " + dependency.GetType().ToString() + " <Elapsed Time: " + elapsedTime.ToString("F3") + " s>");
			StatsManager.Instance.LogLoadTimeEnd("dep_" + dependency.GetType().ToString());

			// Now that we're complete, re-evaluate all dependencies to see if they need to be inited
			InitializeUnblockedDependencies();
		}
	}
	
	/// <summary>
	/// Updates the state of the init.
	/// </summary>
	private void InitializeUnblockedDependencies() 
	{
		Bugsnag.LeaveBreadcrumb("InitializationManager - InitializeUnblockedDependencies() beginning");
		List<IDependencyInitializer> dependenciesToInit = new List<IDependencyInitializer>();

		// Loop through each dependency that we're aware of
		foreach (var pair in _dependencies) 
		{
			IDependencyInitializer dep = pair.Key;
			InitializationState state = pair.Value;
			
			if (dep == null || state == null)
			{
				Debug.LogError(string.Format("InitializationManager.InitializeUnblockedDependencies(): Bad dependencies data for {0}:{1}", dep, state));
				continue;
			}
			
			// Check to see if we've started initializing this dependency
			if (state.started == false) 
			{
				bool satisfied = true;
				
				Type[] depTypes = dep.GetDependencies();
				if (depTypes != null && depTypes.Length > 0)
				{
					// Iterate through each dependency in the list, checking to see if it's already been initialized
					// If all have been initialized (none return an init state of false), then it's OK to start
					// initializing the dependency in question
					foreach (Type depType in depTypes) 
					{
						if (depType == null)
						{
							Debug.LogError(string.Format("InitializationManager.InitializeUnblockedDependencies(): Null depType in depTypes for {0}:{1}", dep, state));
						}
						else if (_typesToDependencies.ContainsKey(depType)) 
						{
							IDependencyInitializer ourDep = _typesToDependencies[depType];
							InitializationState ourDepState = _dependencies[ourDep];
							if (ourDepState == null)
							{
								Debug.LogError(string.Format("InitializationManager.InitializeUnblockedDependencies(): Null ourDepState in _dependencies for {0}:{1}", dep, state));
							}
							else if (ourDepState.initialized == false) 
							{
								satisfied = false;
								break;
							}	
						} 
						else 
						{
							Debug.LogError(string.Format("InitializationManager.InitializeUnblockedDependencies(): No instance of {0} for {1}:{2}", depType, dep, state));
						}
					}
				}
				
				// If all dependencies have been initialized, then start the initialization
				if (satisfied) 
				{
					state.started = true;
					state.startTime = Time.realtimeSinceStartup;
					if (Application.isEditor)
					{
						dependencyOrder.Add(initializationRound + "  -  " + dep.description());
					}
					dependenciesToInit.Add(dep);
				}
			}
		}
		initializationRound++;
		foreach (IDependencyInitializer dep in dependenciesToInit)
		{
			if (dep != null)
			{
				//Debug.Log("=== INIT MANAGER: STARTING " + dep.GetType().ToString());
				StatsManager.Instance.LogLoadTimeStart("dep_" + dep.GetType().ToString());
				dep.Initialize(this);
			}
			else
			{
				Debug.LogError("InitializationManager.InitializeUnblockedDependencies(): Null IDependencyInitializer in dependenciesToInit.");
			}
		}
	}

	/// <summary>
	/// Checks the array of types passed-in to see if they, or any of their dependencies, depend on the 
	/// testType passed in the second parameter.
	/// </summary>
	/// <returns>
	/// True if a cycle is detected, false otherwise
	/// </returns>
	/// <param name='depTypes'>
	/// Array containing the set of Types to check for dependencies upon testType
	/// </param>
	/// <param name='testType'>
	/// testType to check
	/// </param>
	private bool DoAnyTypesDependOnType(Type[] depTypes, Type testType) 
	{
		// Declare a return var
		bool retVal = false;
		
		// Iterate through each of the types passed-in.  For each, see if we've already got a type->dependency mapping
		// already defined.  If no dependency exists yet, we assume no cycle exists on that branch of the graph for the
		// moment.
		foreach (Type depType in depTypes) 
		{
			if (_typesToDependencies.ContainsKey(depType)) 
			{
				
				// We have an instance of the type in question... get its dependencies and iterate through them to see
				// if any matches the testType
				IDependencyInitializer currDep = _typesToDependencies[depType];	
				foreach (Type currType in currDep.GetDependencies()) 
				{
					if (currType == testType) 
					{
						// We found a cycle, set the return value and break out of this loop
						retVal = true ;
						break ;
					}
				}
				
				// Make sure we don't continue to loop if we've already found a cycle
				if (retVal) 
				{
					break;
				} else 
				{
					retVal = DoAnyTypesDependOnType(currDep.GetDependencies(), testType);
				}
			}
		}
		
		return retVal;
	}


	// A diagnostic function that returns a sorted textual list of all registered IDependencyInitializer's & their (expanded) dependencies
	// For example:
	//
	//   BasicInfoLoader        Depends On:  
	//   URLStartupManager      Depends On:  
	//   ClientVersionCheck     Depends On:  BasicInfoLoader  
	//   ZdkManager             Depends On:  BasicInfoLoader  ClientVersionCheck  
	//   AuthManager            Depends On:  BasicInfoLoader  ClientVersionCheck  ZdkManager  
	//   StatsManager           Depends On:  BasicInfoLoader  ClientVersionCheck  ZdkManager  AuthManager  
	//   ...
	public static string getAllDependenciesAsText()
	{
		// make dict of  dependencyInitializers : all it's (expanded) dependencies
		var initializerAllDeps = new Dictionary<IDependencyInitializer, List<IDependencyInitializer>>(_dependencies.Count);
		foreach (IDependencyInitializer item in _dependencies.Keys)
		{
			initializerAllDeps[item] = getExpandedDependencies(item);
		}

		// Sort each dependency list so so minimal dependencies are first
		foreach (var dependencyList in initializerAllDeps.Values)
		{
			dependencyList.Sort((item1, item2) => initializerAllDeps[item1].Count.CompareTo( initializerAllDeps[item2].Count));
		}

		// Sort list of initializers by their # of dependencies
		var allInitializers = new List<IDependencyInitializer>(_dependencies.Keys);
		allInitializers.Sort((item1, item2) => initializerAllDeps[item1].Count.CompareTo( initializerAllDeps[item2].Count));

		// Generate a line of text for each item & it's dependencies 
		var sb = new System.Text.StringBuilder("All Initialization Manager items & their dependencies:\n", 4096);
		foreach (IDependencyInitializer initializer in allInitializers) 
		{
			sb.Append( String.Format("  {0,-24} DependsOn:  ", initializer.description()) );
			foreach (IDependencyInitializer dep in initializerAllDeps[initializer])
			{
				sb.Append( dep.description() + "  " );
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}


	// Returns a (recursively expanded) list of all dependencies for a given IDependencyInitializer
	static List<IDependencyInitializer> getExpandedDependencies(IDependencyInitializer dep)
	{
		// A list to accumulate results as we recurse
		var accumulator = new List<IDependencyInitializer>(32);
		getExpandedDependencies_Recurse(dep, accumulator);

		return accumulator;
	}

	static void getExpandedDependencies_Recurse(IDependencyInitializer dep, List<IDependencyInitializer> accumulator)
	{
		foreach (Type depType in dep.GetDependencies()) 
		{
			IDependencyInitializer ourDep = _typesToDependencies[depType];
			if (!accumulator.Contains(ourDep)) // skip redundancies
			{
				accumulator.Add(ourDep); // add this dependency
				getExpandedDependencies_Recurse(ourDep, accumulator); // get+add sub-dependencies
			}
		}
	}

	
	/// Resets the static class data.
	public static void resetStaticClassData() 
	{
		_instance = null;
		_dependencies.Clear();
		_typesToDependencies.Clear();
	}
}


