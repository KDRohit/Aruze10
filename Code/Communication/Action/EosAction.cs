using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/*
{
	"actions": [
		{
			"type": "eos_log_event",
			"experimentName": "hir_buypage_progressive",
			"eventType": "view"
		}
	]
}
 */
public class EosAction : ServerAction
{
	private const string EOS_LOG_EVENT = "eos_log_event";
	private const string HIR_BUYPAGE_PROGRESSIVE = "hir_buypage_progressive";
	private const string VIEW = "view";

	private string experimentName = "";
	private string eventType = "";


	//This string is private and only is set sometimes.  It is inaccessible elsewhere, so it's wasted.  If needed, remove this comment and note 
	//its usage.  Otherwise it is generating an unnecessary error.
	//private string playerId = "";

	//property names
	private const string TYPE = "type";
	private const string EXPERIMENT_NAME = "experimentName";
	private const string EVENT_TYPE = "eventType";
	
	public static void notifyBuypageView()
	{
		string name = HIR_BUYPAGE_PROGRESSIVE; // Assigned to name for copy-pasting ease for the below.
		if (Data.login.getJSON("eos." + name) != null)
		{
			EosAction action = new EosAction(ActionPriority.NORMAL, EOS_LOG_EVENT, name);
			action.eventType = VIEW;
		}
		else
		{
			Debug.LogWarning("EOS experiment does not exist: " + name);
		}
	}
	
	private EosAction(ActionPriority priority, string type, string experimentName) : base(priority, type)
	{
		this.experimentName = experimentName;
	}

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		appendPropertyJSON(builder, TYPE, type);
		appendPropertyJSON(builder, EXPERIMENT_NAME, experimentName);
		appendPropertyJSON(builder, EVENT_TYPE, eventType);
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}

#if UNITY_EDITOR
	public static void testCreateEosAction()
	{
		EosAction action = new EosAction(ActionPriority.NORMAL, HIR_BUYPAGE_PROGRESSIVE, HIR_BUYPAGE_PROGRESSIVE);
		action.eventType = VIEW;
		Dictionary<string, object> json = new Dictionary<string, object>();
		json["testKey"] = "testValue";
		string template =
			"{actions\": [{" +
				"\"type\": \"eos_log_event\"," +
				"\"experimentName\": \"hir_buypage_progressive\"," +
				"\"eventType\": \"view\"" +
			"}]}";
		string output = ServerAction.getBatchStringForTesting(action);
		Debug.Log(string.Format("ServerAction {0}: {1}", action, output));
		if (output != template)
		{
			Debug.LogError(string.Format("ServerAction {0} has invalid serialization {1}: doesn't match {2}", action, output, template));
		}
	}
#endif

}

