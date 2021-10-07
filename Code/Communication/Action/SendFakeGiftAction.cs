using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Sets up gift request-related actions.
*/

public class SendFakeGiftAction : ServerAction 
{
	/// ServerAction type.
	public const string SEND_GIFT = "send_gift_fake";
	
	private string giftType = null;
	private string message = null;
	private List<long> zIds = null;
	private string bonusGame = null;
	private string slotsGame = null;

	//property names
	private const string GIFT_TYPE = "gift_type";
	private const string MESSAGE = "message";
	private const string TO = "to";
	private const string BONUS_GAME = "bonus_game";
	private const string SLOTS_GAME = "slots_game";

	public static void sendGift(long zid, string gameDesignator, string bonusGameName)
	{
		SendFakeGiftAction action = new SendFakeGiftAction(ActionPriority.IMMEDIATE, SEND_GIFT);  
		action.giftType = "gift_bonus";	// Re-use the "type" argument that was sent to Facebook, except it's called "gift_type" for this action.
		List<long> ids = new List<long>();
		ids.Add(zid);
		action.zIds = ids;

		action.message = "This is a gifted free spin game for " + gameDesignator + " sent from the dev panel";
		action.bonusGame = bonusGameName;
		action.slotsGame = gameDesignator;
	}

	public static void sendCreditsGift(long zid, long amount)
	{
		SendFakeGiftAction action = new SendFakeGiftAction(ActionPriority.IMMEDIATE, SEND_GIFT);
		action.giftType = "gift_credits";
		List<long> ids = new List<long>();
		ids.Add(zid);
		action.zIds = ids;
		action.message = "Some Free Credits!";
	}

	public static void sendAskForCredits(long zid)
	{
		SendFakeGiftAction action = new SendFakeGiftAction(ActionPriority.IMMEDIATE, SEND_GIFT);
		action.giftType = "ask_for_credits";
		List<long> ids = new List<long>();
		ids.Add(zid);
		action.zIds = ids;
		action.message = SlotsPlayer.instance.socialMember.firstName + " could use some coins!";
	}

	public static void sendAskForRating(long zid, string gameName = "")
	{
		SendFakeGiftAction action = new SendFakeGiftAction(ActionPriority.IMMEDIATE, SEND_GIFT);
		action.giftType = "ratings";
		action.slotsGame = string.IsNullOrEmpty(gameName) ? "com03" : gameName;
		List<long> ids = new List<long>();
		ids.Add(zid);
		action.zIds = ids;
		action.message = "Select the rating!";
	}

	private SendFakeGiftAction(ActionPriority priority, string type) : base(priority, type)
	{
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
				
				_propertiesLookup.Add(SEND_GIFT, new string[] { GIFT_TYPE, MESSAGE, TO, BONUS_GAME, SLOTS_GAME});
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
				case GIFT_TYPE:
					appendPropertyJSON(builder, property, giftType);
					break;
				case MESSAGE:
					appendPropertyJSON(builder, property, message);
					break;
				case TO:
					appendPropertyJSON(builder, property, CommonText.joinLongs(",", zIds));
					break;
				case BONUS_GAME:
					appendPropertyJSON(builder, property, bonusGame);
					break;
				case SLOTS_GAME:
					appendPropertyJSON(builder, property, slotsGame);
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

