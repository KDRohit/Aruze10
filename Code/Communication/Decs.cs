using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   The Dependent Event Callback system (decs). This class sets up the ability to add events that need to
///   be completed before a specified callback is made. This is similar to web's version of Wait, and Signals.
///   Basic use case is to call Decs.registerEvent(string, DecsDelegate) or Decs.registerEvent(string[] DecsDelegate)
///
///	  You can pass a Dict to registerEvent as part of your callback, and also specify if you want this to only happen
///	  once using the "runOnce" argument. By default the system assumes that each event isn't unique per session, and
///   can be handled more than once
/// </summary>
public class Decs
{
	// =============================
	// PRIVATE
	// =============================
	private static List<DecsEvent> events = new List<DecsEvent>();

	// =============================
	// PUBLIC
	// =============================
	public delegate void DecsDelegate(Dict args = null);

	public static DecsEvent registerEvent(string eventName, DecsDelegate callback = null, Dict callbackArgs = null, bool runOnce = false)
	{
		DecsEvent decsEvent = hasEvent(eventName);
		if (decsEvent == null)
		{
			decsEvent = new DecsEvent(new string[]{eventName}, callback, callbackArgs, runOnce);
			events.Add(decsEvent);
		}
		else
		{
			decsEvent.registerHandler(callback, callbackArgs);
		}

		return decsEvent;
	}

	public static DecsEvent registerEvent(string[] eventNames, DecsDelegate callback = null, Dict callbackArgs = null, bool runOnce = false)
	{
		DecsEvent decsEvent = hasEvent(eventNames);
		if (decsEvent == null)
		{
			decsEvent = new DecsEvent(eventNames, callback, callbackArgs, runOnce);
			events.Add(decsEvent);
		}
		else
		{
			decsEvent.registerHandler(callback, callbackArgs);
		}

		return decsEvent;
	}
	
	private static DecsEvent hasEvent(string eventName)
	{
		for (int i = 0; i < events.Count; ++i)
		{
			if (events[i].hasEventName(eventName))
			{
				return events[i];
			}
		}

		return null;
	}

	private static DecsEvent hasEvent(string[] eventNames)
	{
		for (int i = 0; i < events.Count; ++i)
		{
			if (events[i].hasEventName(eventNames))
			{
				return events[i];
			}
		}

		return null;
	}

	public static void completeEvent(string eventName, bool runOnce = true)
	{
		// add it if it didn't exist before
		DecsEvent decsEvent = registerEvent(eventName, null, null, runOnce);
		
		// complete the event
		decsEvent.completeEvent(eventName);

		if (!decsEvent.runOnce)
		{
			events.Remove(decsEvent);
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	///   Debug/localconfig controlled logging of the errors in Decs
	/// </summary>
	private static void logVerbose(string msg, params object[] args)
	{
		if ( Data.debugMode )
		{
			Debug.LogFormat(msg, args);
		}
	}

	public static string getPendingEvents()
	{
		for (int i = 0; i < events.Count; ++i)
		{
			if (!events[i].isComplete)
			{
				return events[i].ToString();
			}
		}

		return "";
	}
}

/// <summary>
///   Event class that holds multiple event dependencies
/// </summary>
public class DecsEvent
{
	private List<string> events = new List<string>();
	private List<string> completedEvents = new List<string>();

	// callbacks
	private List<Decs.DecsDelegate> onCompleteCallbacks = new List<Decs.DecsDelegate>();
	private List<Dict> onCompleteCallbackArgs = new List<Dict>();

	// set to true when all events have been passed through the completeEvent()
	public bool isComplete {get; private set;}
	public bool runOnce {get; private set;}

	public DecsEvent(string[] events, Decs.DecsDelegate onComplete = null, Dict callbackArgs = null, bool runOnce = true)
	{
		this.events = events.OfType<string>().ToList();
		this.runOnce = runOnce;

		registerHandler(onComplete, callbackArgs);
	}

	public void completeEvent(string eventName)
	{
		if (events.Contains(eventName) && !completedEvents.Contains(eventName))
		{
			completedEvents.Add(eventName);

			// we done!
			if (completedEvents.Count == events.Count)
			{
				isComplete = true;

				for (int i = 0; i < onCompleteCallbacks.Count; ++i)
				{
					onCompleteCallbacks[i](onCompleteCallbackArgs[i]);
				}
			}
		}
	}

	public void registerHandler(Decs.DecsDelegate onComplete = null, Dict callbackArgs = null)
	{
		if (onComplete != null && !onCompleteCallbacks.Contains(onComplete))
		{			
			onCompleteCallbacks.Add(onComplete);
			onCompleteCallbackArgs.Add(callbackArgs);
		}
	}
	
	public bool hasEventName(string name)
	{
		return events.Contains(name);
	}

	public bool hasEventName(string[] names)
	{
		for (int i = 0; i < names.Length; ++i)
		{
			if (events.Contains(names[i]))
			{
				return true;
			}
		}

		return false;
	}

	public override string ToString()
	{
		return string.Join(", ", events.ToArray());
	} 
}
