using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Similar to the DoSomething structure, Inbox has special CTA actions that can be handled here.
/// Inbox's CTA has the ability to do multiple things, and pass in an array of parameters. This idea
/// breaks the base DoSomething structure. As such this class can also be viewed as a wrapper for DoSomething.
/// If no InboxCommand can be found, it will default to attempting to use the DoSomething structure. This
/// gives InboxCommand a lot of flexibility with the same power as the carousel actions
/// </summary>
public class InboxCommand : IResetGame
{
	public string action;
	public string args;
	
	protected static HashSet<string> collectedEvents = new HashSet<string>();

	/// <summary>
	/// Initializes with data from the server for the action (possible do something command) and args
	/// </summary>
	/// <param name="action"></param>
	/// <param name="args"></param>
	public virtual void init(string action, string args)
	{
		this.action = action;
		this.args = args;
	}

	/// <summary>
	/// Execute the command, defaults to dosomething.now()
	/// </summary>
	public virtual void execute(InboxItem inboxItem)
	{
		if (DoSomething.isValidString(action))
		{
			if (DoSomething.shouldCloseInbox(action))
			{
				Dialog.close();	
			}
			DoSomething.now(action);
		}
	}

	/// <summary>
	/// Returns true if the command can be executed. Commands that are using the DoSomething
	/// system will only return true if the DoSomething.getIsValidToSurface() returns true.
	/// Other commands can extend/overwrite this functionality as needed, the default will always return
	/// true otherwise.
	/// </summary>
	public virtual bool canExecute
	{
		get
		{
			if (DoSomething.isValidString(action))
			{
				return DoSomething.getIsValidToSurface(action);
			}

			return true;
		}
	}

	public virtual GameTimer timer
	{
		get
		{
			if (DoSomething.isValidString(action))
			{
				return DoSomething.getTimer(action);
			}

			return null;
		}
	}

	public virtual string getActionValue(string key)
	{
		if (DoSomething.isValidString(action))
		{
			return DoSomething.getValue(action, key);
		}

		return "";
	}

	/// <summary>
	/// Action name is the name of this command
	/// </summary>
	public virtual string actionName
	{
		get { return ""; }
	}

	public static void resetStaticClassData()
	{
		collectedEvents.Clear();
	}

}