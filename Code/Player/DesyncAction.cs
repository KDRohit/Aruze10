// File              : Unity/Assets/Code/Player/DesyncAction.cs
// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 22.05.2019
//
// This action forward server the infomation needed to create the gist and Jira ticket.
// Responsible for displaying the created Jira ticket number upon successful creation

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

public class DesyncAction : ServerAction
{
	private string clientVersion;
	private string desyncGameKey;

	private string previousBatchOutcome; // Instance variable copied from the Static version when a new DesyncAction is made ensuring we don't lose the outcomes we care about
	private string latestBatchOutcome; // Instance variable copied from the Static version when a new DesyncAction is made ensuring we don't lose the outcomes we care about
	private PlayerResource.DesyncCoinFlow flow;
	
	private const string SUBMIT_DESYNC= "submit_desync";
	private const string DESYNC_RESPONSE = "desync_response";
	private const string CLIENT_VERSION = "client_version";
	private const string DESYNC_GAME_KEY = "desync_game_key";
	private const string CLIENT_EXPECTED_CREDITS = "client_expected_credits";
	private const string SERVER_EXPECTED_CREDITS = "server_expected_credits";
	private const string LATEST_BATCH_OUTCOME = "latest_batch_outcome";
	private const string PREVIOUS_BATCH_OUTCOME = "previous_batch_outcome";
	private const string CLIENT_LOGIN_DATA = "client_login_data";
	private const string SUGGESTED_SOURCE = "suggested_source";

	private static string previousStaticBatchOutcome; // Static version updated when new slots_outcome comes down from server
	private static string latestStaticBatchOutcome; // Static version updated when new slots_outcome comes down from server
	
	// Set by PlayerResource.checkResourceChange
	public static long clientExpectedCredits;
	public static long serverExpectedCredits;
	
	// handle response from Server, and display infomation accordingly
	protected static void processResponse(JSON data)
	{
		string jiraTicketKey = data.getString("jira_key", "FAILED");
		string message;
		
		message = string.Format("Created Issue for desync: {0}", jiraTicketKey);

		Dict dict = Dict.create(
				D.TITLE, "Submitted Jira Issue",
				D.REASON, "jira_created",
				D.MESSAGE, message);

		GenericDialog.showDialog(dict, SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	// currently only called in Server.cs to record slotsOutcomes
	public static void receiveServerOutcomes(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			JSON serverResponseJson = new JSON(message);
			JSON[] events = serverResponseJson.getJsonArray("events");
			bool isSlotOutcome = false;
			for (int i = 0; i < events.Length; i++)
			{
				JSON currentEvent = events[i];
				string typeString = currentEvent.getString("type", "");
				if (typeString == "slots_outcome")
				{
					isSlotOutcome = true;
					break;
				}
			}
			if (isSlotOutcome)
			{
				previousStaticBatchOutcome = latestStaticBatchOutcome;
				latestStaticBatchOutcome = message;
			}
		}
	}
	
	// getting called in Data.cs
	public static void onLogin()
	{
		previousStaticBatchOutcome = "";
		latestStaticBatchOutcome = "";
	}

	private DesyncAction(ActionPriority priority, string type) : base(priority, type)
	{
		previousBatchOutcome = string.IsNullOrEmpty(previousStaticBatchOutcome) ? "None" : previousStaticBatchOutcome;
		latestBatchOutcome = latestStaticBatchOutcome;
	}

	private static void initPropertiesLookup()
	{
		_propertiesLookup = new Dictionary<string, string[]>();
		_propertiesLookup.Add(SUBMIT_DESYNC, new string[] {CLIENT_VERSION, DESYNC_GAME_KEY, SERVER_EXPECTED_CREDITS, 
			CLIENT_EXPECTED_CREDITS, LATEST_BATCH_OUTCOME, PREVIOUS_BATCH_OUTCOME, CLIENT_LOGIN_DATA, SUGGESTED_SOURCE});
	}

	// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				initPropertiesLookup();
			}
			return _propertiesLookup;
		}
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case CLIENT_VERSION:
					appendPropertyJSON(builder, property, clientVersion);
					break;
				case DESYNC_GAME_KEY:
					appendPropertyJSON(builder, property, desyncGameKey);
					break;
				case SERVER_EXPECTED_CREDITS:
					appendPropertyJSON(builder, property, serverExpectedCredits);
					break;
				case CLIENT_EXPECTED_CREDITS:
					appendPropertyJSON(builder, property, clientExpectedCredits);
					break;
				case LATEST_BATCH_OUTCOME:
					appendPropertyJSON(builder, property, latestBatchOutcome);
					break;
				case PREVIOUS_BATCH_OUTCOME:
					appendPropertyJSON(builder, property, previousBatchOutcome);
					break;
				case CLIENT_LOGIN_DATA:
					appendPropertyJSON(builder, property, Data.login.ToString());
					break;
				case SUGGESTED_SOURCE:
					appendPropertyJSON(builder, property, flow != null ? flow.source : "unknown");
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}
	
	public static void reportDesyncError(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "2")
		{
			submitDesyncJira();
		}
	}

	// getting required data filled and register the event 
	private static void submitDesyncJira(PlayerResource.DesyncCoinFlow flow = null)
	{
		DesyncAction action = new DesyncAction(ActionPriority.HIGH, SUBMIT_DESYNC);
		
		if (GameState.game != null)
		{
			action.desyncGameKey = GameState.game.keyName;
		}

		action.flow = flow;
		action.clientVersion = Glb.clientVersion;

		Server.registerEventDelegate(DESYNC_RESPONSE, processResponse);
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	public static void resetStaticClassData()
	{
		_propertiesLookup = null;
	}
}
