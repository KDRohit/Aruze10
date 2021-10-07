using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/**
This is the base class for all action types that are used for communication client actions to the server.
*/
public class ServerAction : IResetGame
{
	public static int THRESHOLD_ACTIONS = 1000;			///< Post the update list when reaching this number of actions.

	public static float SERVER_UPDATE_HIGH_PRIORITY = 1f;	///< Update time-to-send with high priority actions
	public static float SERVER_UPDATE_NORMAL_PRIORITY = 5f;	///< Update time-to-send with normal priority actions
	public static float SERVER_UPDATE_LOW_PRIORITY = 20f;	///< Update time-to-send with low priority actions
	public static float SERVER_UPDATE_NO_PRIORITY = 60f;	///< Update time-to-send with no pending actions

	public static float SERVER_FAST_UPDATE_TIME = 2f; 		///< Post the update list after this amount of time while in fast update mode
    public static float SERVER_FAST_UPDATE_PERIOD = 30f;    ///< Max amount of time we can be in fast update mode
    
    public static string READ_ONLY_ACTION_EXPERIMENT = "read_only_actions";    ///< experiment name for read only actions


    public int sortOrder;			///< A unique sequential value to identify the action.
	public string type;				///< The type of action, as taken from the list of constants at the top of the class.

	public static ActionPriority batchPriority = ActionPriority.NONE;	///< The priority of the current batch of actions
	private static int nextActionSortOrder = 1;							///< Increases by 1 with each new action.

	public static float lastBatchTime = Time.realtimeSinceStartup;
	public static float lastActionTime = Time.realtimeSinceStartup;

	public static bool enableCommunication = true;	// Is communication enabled? Set with extreme caution.

	private static List<ServerAction> pendingActions = new List<ServerAction>();

	private static float fastUpdateTimer;					///< When > 0 we're in fast update mode

	private static Dictionary<string, bool> actionDict = null;

	// Tracks if the server has sent out a spin action and is still awaiting a response to it
	public static bool isSpinActionQueued
	{
		get { return _isSpinActionQueued; }
		private set { _isSpinActionQueued = value; }
	}
	private static bool _isSpinActionQueued = false;

	/// The action that we're watching for - ends fast update when received
	public static string fastUpdateActionEventType
	{
		get
		{
			return fastUpdateTimer > 0f ? _fastUpdateActionEventType : "";
		}
		set
		{
			_fastUpdateActionEventType = value;
		}
	}
	private static string _fastUpdateActionEventType = "";

	/// Number of pending actions
	public static int pendingActionCount
	{
		get
		{
			return pendingActions.Count;
		}
	}

	/// Prevent use of default action initializer
	private ServerAction()
	{

    }

    /// <summary>
    /// Check if an action type is a read only action
    /// Read only actions do not save player state when they are done executing
    /// Hence, the sort order need to stay the same
    /// Sort order is a counter that client and server maintain
    /// gets set when zid_login is called and is incremented with each action
    /// since the player is not saved after a read only action, a repeat will not have an impact
    /// 
    /// </summary>
    /// <returns><c>true</c>, if read only action, <c>false</c> otherwise.</returns>
    /// <param name="actionType">type form the action data</param>
    private bool isReadOnlyAction(string actionType) {

        ServerActionExperiment eos = ExperimentManager.GetEosExperiment(READ_ONLY_ACTION_EXPERIMENT) as ServerActionExperiment;

        if (eos == null)
        {
            return false;
        }

        if (!eos.isInExperiment)
        {
            return false;
		}

        string actions = eos.actions;

        if (actions.Length != 0 && ServerAction.actionDict == null)
        {
            string[] actionsArray = actions.Split(new Char[] { ',' });
            ServerAction.actionDict = new Dictionary<string, bool>();
			foreach(string action in actionsArray)  {
				ServerAction.actionDict.Add(action, true);
			}
		}
        return ServerAction.actionDict.ContainsKey(actionType);
	}


	/// Never called directly.
	public ServerAction(ActionPriority actionPriority, string type)
	{
		if (type == SlotAction.SLOTS_SPIN)
		{
			if (isSpinActionQueued)
			{
				// log an error about having more than one spin action queued
				Debug.LogError("ServerAction.ServerAction() - Spin request is already queued, ignoring this request, we should ensure that multiple spin requests aren't made before getting a response!");
				return;
			}
			else
			{
				// mark that we have a spin action queued now
				isSpinActionQueued = true;
			}
		}

        //only increment if its not a read only action
        if (!this.isReadOnlyAction(type)) {
            this.sortOrder = nextActionSortOrder++;
        } else {
            this.sortOrder = nextActionSortOrder - 1; //keep the previous sort
        }


        if (batchPriority < actionPriority)
		{
			// Raise the overall batch priority if it is lower
			batchPriority = actionPriority;

			// Reset the batch timer when bumping up the batch priority
			lastBatchTime = Time.realtimeSinceStartup;
		}

		if (batchPriority == actionPriority)
		{
			// Reset the action timer when the priority matches the batch priority
			lastActionTime = Time.realtimeSinceStartup;
		}

		this.type = type;

		pendingActions.Add(this);
		pendingActions.Sort(sortBySortOrder);

		if (Data.debugMode)
		{
			System.Text.StringBuilder dump = new System.Text.StringBuilder();
			dump.AppendFormat("Dumping Pending ServerAction list, count = {0}", pendingActions.Count);
			dump.AppendLine();

			foreach(ServerAction action in pendingActions) 
			{
				dump.AppendFormat("{0}) {1}", action.sortOrder, action.type);
				dump.AppendLine();
			}
			Debug.LogFormat("PN> {0}", dump.ToString());
		}
	}
	
	private static int sortBySortOrder(ServerAction a, ServerAction b)
	{
		return a.sortOrder.CompareTo(b.sortOrder);
	}

	/// This is called once each loop from the main script to send the server actions.
	/// This is also used as a keep-alive and to get server events.
	public static void processPendingActions(bool force = false)
	{
		// If we are offline or not logged in, clear out the action queue and abort.
		if (!enableCommunication || !SlotsPlayer.isLoggedIn)
		{
			pendingActions.Clear();
			isSpinActionQueued = false;
			return;
		}
		
		// If we are waiting for the server, then nothing to see here.
		if (Server.waitingForActionsResponse)
		{
			return;
		}

		bool doActionUpdate = force || (Server.actionEvents.Count > 0);

		if (!doActionUpdate)
		{
			float elapsedBatchTime = Time.realtimeSinceStartup - lastBatchTime;
			float elapsedActionTime = Time.realtimeSinceStartup - lastActionTime;

			switch (batchPriority)
			{
				case ActionPriority.NONE:
					doActionUpdate = doActionUpdate || (elapsedActionTime > SERVER_UPDATE_NO_PRIORITY);
					doActionUpdate = doActionUpdate || (elapsedBatchTime > SERVER_UPDATE_NO_PRIORITY);
					break;
				case ActionPriority.LOW:
					doActionUpdate = doActionUpdate || (elapsedActionTime > SERVER_UPDATE_LOW_PRIORITY);
					doActionUpdate = doActionUpdate || (elapsedBatchTime > SERVER_UPDATE_NO_PRIORITY);
					break;
				case ActionPriority.NORMAL:
					doActionUpdate = doActionUpdate || (elapsedActionTime > SERVER_UPDATE_NORMAL_PRIORITY);
					doActionUpdate = doActionUpdate || (elapsedBatchTime > SERVER_UPDATE_LOW_PRIORITY);
					break;
				case ActionPriority.HIGH:
					doActionUpdate = doActionUpdate || (elapsedActionTime > SERVER_UPDATE_HIGH_PRIORITY);
					doActionUpdate = doActionUpdate || (elapsedBatchTime > SERVER_UPDATE_NORMAL_PRIORITY);
					break;
				case ActionPriority.IMMEDIATE:
					doActionUpdate = true;
					break;
			}

			doActionUpdate = doActionUpdate || (pendingActionCount > THRESHOLD_ACTIONS);
			doActionUpdate = doActionUpdate || (fastUpdateTimer > 0f && elapsedBatchTime > SERVER_FAST_UPDATE_TIME);
		}

		if (doActionUpdate)
		{
			// Post all the actions as a single JSON string.
			string json = getBatchString();

			lastBatchTime = Time.realtimeSinceStartup;
			lastActionTime = Time.realtimeSinceStartup;
			batchPriority = ActionPriority.NONE;
			pendingActions.Clear();
			isSpinActionQueued = false;

			RoutineRunner.instance.StartCoroutine(Server.postActions(json));

			// Clear fast update mode if the period for it is up
			if (fastUpdateTimer > 0f)
			{
				float fastElapsed = Time.realtimeSinceStartup - fastUpdateTimer;
				float maxElapsed = (fastUpdateActionEventType == "") ? SERVER_FAST_UPDATE_PERIOD * 0.5f : SERVER_FAST_UPDATE_PERIOD;
				if (fastElapsed > maxElapsed)
				{
					clearFastUpdateMode();
				}
			}
		}
	}

	public static string getBatchString()
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		builder.Append("{\"actions\":[");

		bool first = true;
		foreach (ServerAction action in pendingActions)
		{
			if (first)
			{
				first = false;
			}
			else
			{
				builder.Append(",");
			}

			builder.Append("{");
			action.appendCommonJSON(builder);
			action.appendSpecificJSON(builder);
			builder.Append("}");
#if !ZYNGA_PRODUCTION
			if (ActionHistory.isRecording)
			{
				// Only do this if we are actively recording things.
				System.Text.StringBuilder jsonBuilder = new System.Text.StringBuilder();
				jsonBuilder.Append("{");
				action.appendCommonJSON(jsonBuilder);
				action.appendSpecificJSON(jsonBuilder);
				jsonBuilder.Append("}");
				ActionHistory.addSentAction(action.type, new JSON(jsonBuilder.ToString()));
			}
#endif
		}

		builder.Append("]}");

		return builder.ToString();
	}

	/// This is only called on page exit to catch the last pending actions.
	/// Calling this during normal continuous gameplay will break stuff.
	public static void flushActionsForExit()
	{
		if (!Server.waitingForActionsResponse && pendingActionCount > 0)
		{
			string json = getBatchString();
			pendingActions.Clear();
			isSpinActionQueued = false;
			Dictionary<string, string> elements = Server.collectActionElements(json, false);
			if (elements == null)
			{
				Debug.LogError("ServerAction.flushActionsForExit() failed to collect action elements.");
				return;
			}
			Server.getRequestWWW(Glb.actionUrl, elements);
		}
	}

	/// Appends a single value to the StringBuilder in a JSON key-value format.
	/// Appended text always starts with a comma for simplicity everywhere this is used.
	public void appendPropertyJSON(System.Text.StringBuilder builder, string property, object value)
	{
		if (value == null)
		{
			return;
		}

		builder.Append(",");
		builder.Append(JSON.createJsonString(property, value));
	}

	/// Appends a literal string without modification (i.e. no quotes added)
	public void appendPropertyLiteralJSON(System.Text.StringBuilder builder, string property, string value)
	{
		builder.Append(",\"");
		builder.Append(property);
		builder.Append("\":");
		builder.Append(value);
	}

	/// Appends the common JSON that all actions must have, including the loot block.
	private void appendCommonJSON(System.Text.StringBuilder builder)
	{
		builder.Append("\"sort_order\":");
		builder.Append(sortOrder.ToString());
		builder.Append(",\"type\":\"");
		builder.Append(type);
		builder.Append("\"");
	}

	/// This must be overridden in any Action derived class
	public virtual void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		
	}

	public static void setFastUpdateMode(string targetEventType)
	{
		fastUpdateTimer = Time.realtimeSinceStartup;
		fastUpdateActionEventType = targetEventType;
	}

	public static void clearFastUpdateMode()
	{
		fastUpdateTimer = 0f;
		fastUpdateActionEventType = "";
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		pendingActions = new List<ServerAction>();
		enableCommunication = true;
		_isSpinActionQueued = false;
	}

	/// This is a helper coroutine to wait for an action batch to complete
	public static IEnumerator waitForActionBatch()
	{
		while (Server.waitingForActionsResponse || pendingActionCount > 0)
		{
			if (!Server.waitingForActionsResponse && pendingActionCount > 0)
			{
				ServerAction.processPendingActions(true);
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

#if UNITY_EDITOR
	// For testing
	public static string getBatchStringForTesting(ServerAction action)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		builder.Append("{");
		action.appendCommonJSON(builder);
		action.appendSpecificJSON(builder);
		builder.Append("}");
		return builder.ToString();
	}

	public static void resetForTesting()
	{
		nextActionSortOrder = 1;
		resetStaticClassData();
	}
#endif

}

public enum ActionPriority
{
	NONE = 0,
	LOW = 1,
	NORMAL = 2,
	HIGH = 3,
	IMMEDIATE = 4
}
