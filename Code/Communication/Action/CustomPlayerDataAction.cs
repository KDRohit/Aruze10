using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling the Custom Player Data Actions
*/
public class CustomPlayerDataAction : ServerAction
{
	public const string CUSTOM_PLAYER_FIELD = "set_custom_player_field";

	private string customPlayerFieldKey = "";
	private object customPlayerFieldValue;

	//property names
	private const string CUSTOM_PLAYER_FIELD_KEY = "field_name";
	private const string CUSTOM_PLAYER_FIELD_VALUE = "value";


	private CustomPlayerDataAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// We need to store each value as the native type it is,
	// since the server is very specific about validating the data for its defined type.
	
	// This should not be called directly. Call CustomPlayerData.setValue() instead.
	public static void setCustomPlayerField(string name, object value)
	{
		CustomPlayerDataAction action = new CustomPlayerDataAction(ActionPriority.HIGH, CUSTOM_PLAYER_FIELD);
		action.type = CUSTOM_PLAYER_FIELD;
		action.customPlayerFieldKey = name;
		action.customPlayerFieldValue = value;
	}
	
	////////////////////////////////////////////////////////////////////////

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(CUSTOM_PLAYER_FIELD, new string[]{CUSTOM_PLAYER_FIELD_KEY, CUSTOM_PLAYER_FIELD_VALUE});
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		string[] propertiesArray;
		if (!propertiesLookup.TryGetValue(type, out propertiesArray))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		for(int i=0; i<propertiesArray.Length; i++)
		{
			string property = propertiesArray[i];
			switch (property)
			{
				case CUSTOM_PLAYER_FIELD_KEY:
					appendPropertyJSON(builder, property, customPlayerFieldKey);
					break;
				case CUSTOM_PLAYER_FIELD_VALUE:
					appendPropertyJSON(builder, property, customPlayerFieldValue);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}
