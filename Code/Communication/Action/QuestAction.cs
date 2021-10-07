using UnityEngine;
using System.Collections.Generic;

/*
{
  "actions": [
    {
      "sort_order": -1,
      "type": "quest_update"    
    }
  ]
}
*/

public class QuestAction : ServerAction
{
	private const string QUEST_UPDATE_ACTION_NAME = "quest_update";  // Note the returned EVENT is called "quests_update"   (confusing)
	
	private QuestAction(ActionPriority priority, string action_name) : base(priority, action_name)
	{
	}

	// just sends generic string action request to server
	public static void requestServerAction(string action_name)
	{
		new QuestAction(ActionPriority.HIGH, action_name);
	}

	// handy method for QUEST_UPDATE_ACTION_NAME
	public static void getQuestUpdate()
	{
		//adds to pending server on construction
		new QuestAction(ActionPriority.HIGH, QUEST_UPDATE_ACTION_NAME);
	}
		
	// dont need properties for this action (yet)
#if false
	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				// TODO: would have to fix this to use action 'type'
				_propertiesLookup.Add(/*QUEST_UPDATE_ACTION_NAME*/, new string[] { });
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}
#endif

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}
}
