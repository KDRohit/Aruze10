using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
for handling Premium werver actions.
*/

public class PremiumSliceAction : ServerAction
{
	/** Action Names **/
	private const string DEV_GRANT_SLICE = "premium_slice_grant";

	private const string DEV_SET_LAST_ROUND = "premium_slice_set_last_round";

	/****** Action Variables *****/
	private string eventId = "";
	private long sliceValue = 0;
	private string packageKey = "";

	/***** Property Names *****/
	private const string EVENT_ID = "event";
	private const string SLICE_VALUE = "premium_slice_val";
	private const string PACKAGE_KEY = "premium_slice_key";

	/** Constructor */
	private PremiumSliceAction(ActionPriority priority, string type) : base(priority, type)
	{	
	}

	public static void devGrantPremiumSlice(string key)
	{
		PremiumSliceAction act = new PremiumSliceAction(ActionPriority.IMMEDIATE, DEV_GRANT_SLICE);
		act.packageKey = key;
		processPendingActions();
	}

	public static void devSetLastRound()
	{
		PremiumSliceAction act = new PremiumSliceAction(ActionPriority.IMMEDIATE, DEV_SET_LAST_ROUND);
		processPendingActions();
	}


	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(DEV_GRANT_SLICE, new string[] { PACKAGE_KEY } );
				_propertiesLookup.Add(DEV_SET_LAST_ROUND, new string[] { });
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
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
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventId);
					break;
				case SLICE_VALUE:
					appendPropertyJSON(builder, property, sliceValue);
					break;
				case PACKAGE_KEY:
					appendPropertyJSON(builder, property, packageKey);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}

	}
}


