using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Zynga.Core.Util;

/// <summary>
/// Factory class for inbox commands
/// </summary>
public class InboxCommandGenerator
{
	public static readonly Dictionary<string, InboxCommand> customCommands = new Dictionary<string, InboxCommand>();

	/// <summary>
	/// Initializes and stores all sub class inbox commands
	/// </summary>
	public static void init()
	{
		//we don't ever need to do this more than once
		if (customCommands.Count > 0)
		{
			return;
		}

		Assembly asm = ReflectionHelper.GetAssemblyByName(ReflectionHelper.ASSEMBLY_NAME_MAP[ReflectionHelper.ASSEMBLIES.PRIMARY]);

		if (asm != null)
		{
			foreach (var type in asm.GetTypes())
			{
				if (typeof(InboxCommand).IsAssignableFrom(type))
				{
					InboxCommand command = Activator.CreateInstance(type) as InboxCommand;
					if (command != null)
					{
						if (customCommands.ContainsKey(command.actionName))
						{
							Debug.LogError("Duplicate inbox action name " + command.actionName);
						}

						customCommands[command.actionName] = command;
					}
				}
			}
		}
	}

	/// <summary>
	/// Returns an InboxCommand instance based on the inbox action data
	/// </summary>
	/// <param name="commandData"></param>
	/// <returns></returns>
	public static InboxCommand generateCommand(JSON commandData)
	{
		if (commandData != null)
		{
			string action = commandData.getString("action", "");

			if (!string.IsNullOrEmpty(action))
			{
				InboxCommand commandInstance = findCommandForAction(action);

				if (commandInstance != null)
				{
					Type t = commandInstance.GetType();
					InboxCommand command = Activator.CreateInstance(t) as InboxCommand;
					command.init(action, commandData.getString("args", ""));
					return command;
				}
			}
		}

		return null;
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Finds an InboxCommand class that has the actionName matching the action sent from server data
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	private static InboxCommand findCommandForAction(string action)
	{
		//Check if a custom inbox command class exists
		if (customCommands != null)
		{
			InboxCommand foundCommand = null;
			if (customCommands.TryGetValue(action, out foundCommand))
			{
				return foundCommand;
			}
		}

		//Default to a generic command that goes through the do-something class heiarchy
		return new InboxCommand();
	}
}