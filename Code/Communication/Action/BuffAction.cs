using UnityEngine;
using System.Collections.Generic;

/*
{
  "actions": [
    {
      "sort_order": -1,
      "type": "get_static_buffs_data"    
    }
  ]
}
*/

public class BuffAction : ServerAction
{
	private const string GET_STATIC_BUFFS_DATA_ACTION_NAME = "get_static_buffs_data";  
	
	private BuffAction(ActionPriority priority, string action_name) : base(priority, action_name)
	{
	}

	// handy method for GET_STATIC_BUFFS_DATA_ACTION_NAME
	public static void getStaticBuffsData()
	{
		new BuffAction(ActionPriority.IMMEDIATE, GET_STATIC_BUFFS_DATA_ACTION_NAME);

		Buff.log("BuffAction created: name:{0}", GET_STATIC_BUFFS_DATA_ACTION_NAME);
	}
		
	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}
}
