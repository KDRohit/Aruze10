using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreditAction : ServerAction
{
	public const string ACCEPT_CREDITS = "accept_credits";
	public const string ACCEPT_REFUNDED_CREDITS = "accept_refunded_credits";
	public const string ACCEPT_PURCHASED_ITEM = "accept_purchased_item";
    public const string ACCEPT_FLASH_TO_WEBGL_CREDITS = "accept_flash_to_webgl_credits";
	public const string TIMER_CLAIM = "timer_claim3";
	public const string CLAIM_INVITE_INCENTIVE = "claim_invite_incentive";
	public const string CLAIM_CHARMS_CLOSING_CREDITS = "claim_charms_closing_credits";
	public const string SET_TIMER_CLAIM_DAY = "set_timer_claim_day";
    public const string TIME = "time";
    public const int TIME_UNTIL_NEXT_COLLECT = 10;


	private string eventId = "";
	private int dayToClaim = -1;
	private string bonusStr = "bonus";
	private int payoutNumber = -1;
	private bool isAutoCollect = false;
	private bool isPetCollect = false;

	//property names
	private const string EVENT_ID = "event";
	private const string DAY_TO_CLAIM = "day";
	private const string BONUS_STR = "timer";
	private const string PAYOUT_NUMBER = "payout_number";
	private const string AUTO_COLLECT = "auto_collect";
	private const string PET_COLLECT = "virtual_pet_collect";

	private CreditAction(ActionPriority priority, string type) : base(priority, type) 
	{
	}

	public static void acceptChallengeCredits(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, ACCEPT_CREDITS);
		action.eventId = eventId;
	}

	public static void acceptRefundedCredits(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, ACCEPT_REFUNDED_CREDITS);
		action.eventId = eventId;
	}

	public static void acceptPurchasedItem(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, ACCEPT_PURCHASED_ITEM);
		action.eventId = eventId;
	}

	public static void acceptFlashToWebglCredits(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, ACCEPT_FLASH_TO_WEBGL_CREDITS);
		action.eventId = eventId;
	}

	public static void acceptCharmsRetiredCredits(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, CLAIM_CHARMS_CLOSING_CREDITS);
		action.eventId = eventId;
	}	

	public static void claimTimerCredits(int payoutNumber = -1, string bonusStr = "bonus", bool autoCollect = false, bool isPetCollect = false)
	{
		string typeString = TIMER_CLAIM;

		//used to be ActionPriority.IMMEDIATE, now HIGH with a call to processPendingActions
		CreditAction action = new CreditAction(ActionPriority.HIGH, typeString);
		action.bonusStr = bonusStr;
		action.isAutoCollect = autoCollect;
		action.isPetCollect = isPetCollect;

		if (payoutNumber != -1)
		{
			action.payoutNumber = payoutNumber;
		}

		// Let the timer know that an action has gone out, but the response is pending.
		DailyBonusGameTimer.markTimerActionCalled();
		processPendingActions(true);
	}

	public static void claimInviteIncentive(string eventId)
	{
		CreditAction action = new CreditAction(ActionPriority.HIGH, CLAIM_INVITE_INCENTIVE);
		action.eventId = eventId;			
	}

	public static void setTimerClaimDay(int day, string bonusString = "")
	{
		string claimType = SET_TIMER_CLAIM_DAY;
		
		//used to be ActionPriority.IMMEDIATE, now HIGH with a call to processPendingActions
        CreditAction action = new CreditAction(ActionPriority.HIGH, claimType);
		if (!bonusString.IsNullOrWhiteSpace())
		{
			action.bonusStr = bonusString;
		}
		action.dayToClaim = day;
		ServerAction.processPendingActions(true);
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(ACCEPT_CREDITS, new string[] { EVENT_ID });
				_propertiesLookup.Add(ACCEPT_REFUNDED_CREDITS, new string[] { EVENT_ID });
				_propertiesLookup.Add(ACCEPT_PURCHASED_ITEM, new string[] { EVENT_ID });
				_propertiesLookup.Add(TIMER_CLAIM, new string[] { BONUS_STR, PAYOUT_NUMBER, PET_COLLECT });
				_propertiesLookup.Add(CLAIM_INVITE_INCENTIVE, new string[] { EVENT_ID });
				_propertiesLookup.Add(SET_TIMER_CLAIM_DAY, new string[] { BONUS_STR, DAY_TO_CLAIM });
				_propertiesLookup.Add(ACCEPT_FLASH_TO_WEBGL_CREDITS, new string[] { EVENT_ID });
				_propertiesLookup.Add(CLAIM_CHARMS_CLOSING_CREDITS, new string[] { EVENT_ID });
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

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
					if (eventId != "")
					{
						appendPropertyJSON(builder, property, eventId);
					}
					break;
				case PAYOUT_NUMBER:
					if (payoutNumber != -1)
					{
						appendPropertyJSON(builder, property, payoutNumber);
					}
					break;	
				case BONUS_STR:
					appendPropertyJSON(builder, property, bonusStr);
					break;	
				case DAY_TO_CLAIM:
					if (dayToClaim != -1)
					{
						appendPropertyJSON(builder, property, dayToClaim);
					}
					break;	
                case TIME:
                    appendPropertyJSON(builder, property, TIME_UNTIL_NEXT_COLLECT);
                    break;
				case AUTO_COLLECT:
					appendPropertyJSON(builder, property, isAutoCollect);
					break;
				case "force":
					appendPropertyJSON(builder, property, 1);
					break;
				case PET_COLLECT:
					appendPropertyJSON(builder, property, isPetCollect);
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
