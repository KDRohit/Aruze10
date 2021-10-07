using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PromoCodeAction : ServerAction
{
	private string code = "";
	private string experimentName = "";
	
	private const string EXPERIMENT = "experiment";
	private const string UNLOCK_CODE = "unlock_code";
	private static Dictionary<string, string[]> _propertiesLookup = null;

	private const string CHALLENGE_CAMPAIGN_UNLOCK_ENTRY = "challenge_campaign_unlock_entry";
	
    private PromoCodeAction(ActionPriority priority, string type) : base(priority, type)
    {
        
    }

	public static void sendCodeValidated(string code, string experiment)
	{
		PromoCodeAction action = new PromoCodeAction(ActionPriority.HIGH, CHALLENGE_CAMPAIGN_UNLOCK_ENTRY);
		action.code = code;
		action.experimentName = experiment;
	}

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
				 case EXPERIMENT:
                    if ( !string.IsNullOrEmpty( experimentName ) )
                    {
                        appendPropertyJSON(builder, property, experimentName);
                    }
                    break;

				case UNLOCK_CODE:
                    if ( !string.IsNullOrEmpty( code ) )
                    {
                        appendPropertyJSON(builder, property, code);
                    }
                    break;
					
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(CHALLENGE_CAMPAIGN_UNLOCK_ENTRY, new string[] {EXPERIMENT, UNLOCK_CODE});
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		// Nothing do to?
	}
}