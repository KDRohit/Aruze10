
using System.Collections;
using System.Collections.Generic;

/**
for handling Land of Oz server actions.
*/
public class LOZAction : ServerAction
{
	//Server Action Types
	private const string UPDATE = "get_achievement_progress";
	private const string DEBUG_COMPLETE = "complete_achievement";

	private string gameKey = "";

	//property names
	private const string FEATURE = "feature";
	private const string GAME = "game";

	private LOZAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Get a progress update for a specific game
	public static void getUpdate(string gameKey)
	{
		LOZAction action = new LOZAction(ActionPriority.HIGH, LOZAction.UPDATE);
       	action.gameKey = gameKey;
	}

    // Get a progress update for all game in the land of oz
	public static void getUpdate()
	{
		LOZAction action = new LOZAction(ActionPriority.HIGH, LOZAction.UPDATE);
	}

    // Pass a specific game to be completed
	public static void forceComplete(string gameKey)
	{
		LOZAction action = new LOZAction(ActionPriority.HIGH, LOZAction.DEBUG_COMPLETE);
		action.gameKey = gameKey;
	}

    // Completes every game in the land of oz
    public static void forceComplete()
	{
		LOZAction action = new LOZAction(ActionPriority.HIGH, LOZAction.DEBUG_COMPLETE);
		action.gameKey = "all";
	}

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
        if ( !string.IsNullOrEmpty( gameKey ) )
        {
            appendPropertyJSON( builder, GAME, gameKey );
        }
        else
        {
            appendPropertyJSON( builder, FEATURE, "land_of_oz" );
        }
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
	}
}
