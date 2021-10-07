using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyPageCardEvent : FeatureBase
{
	public static BuyPageCardEvent instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<BuyPageCardEvent>("buy_page_card_event");
		}
	}

	// Currently hard coding this to be a specific sale folder path in s3
	// If we want this to read from the buy_page EOS that works too, but the value didn't exist there.
	public string buyPageHeaderPath
	{
		get
		{
			if (cardEvent == CreditPackage.CreditEvent.MORE_RARE_CARDS)
			{
				return BUY_PAGE_HEADER_IMAGE_FOLDER_MORE_RARE_CARDS;
			}
			else
			{
				return BUY_PAGE_HEADER_IMAGE_FOLDER_MORE_CARDS;
			}

		}
	}

	public string buyPageHeaderTitle
	{
		get
		{
			return Localize.textTitle(BUY_PAGE_HEADER_TITLE);
		}
	}

	public bool shouldShowHeader
	{
		get
		{
			// Dont show the header if we have anything else going on
			return isEnabled &&
			       PurchaseFeatureData.findBuyCreditsMultiplier() <= 1 &&
			       !ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&
			       !ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive &&
			       !CreditSweepstakes.isActive;
		}
	}
	
	public string maxCardEventBonus = "";
	public bool hasMixedCardEventBonuses = false;
	public CreditPackage.CreditEvent cardEvent = CreditPackage.CreditEvent.NOTHING;

	private GameTimerRange eventTimer;
	private bool hasBeenSetup = false;

	// Live Data Keys
	private const string COLLECTIONS_CARDS_LIFT_START_TIME = "COLLECTIONS_CARDS_LIFT_START_TIME";
	private const string COLLECTIONS_CARDS_LIFT_END_TIME = "COLLECTIONS_CARDS_LIFT_END_TIME";
	
	private const string BUY_PAGE_HEADER_TITLE = "limited_time_offer_title";	// Localization Key
	private const string BUY_PAGE_HEADER_IMAGE_FOLDER_MORE_CARDS = "stud/two_for_one_sales/HIR/collections_morecards";
	private const string BUY_PAGE_HEADER_IMAGE_FOLDER_MORE_RARE_CARDS = "stud/two_for_one_sales/HIR/collections_rarecards";

	public void setPackageEvents(string eventData, string eventLiftData)
	{
		if (hasBeenSetup)
		{
			Debug.LogErrorFormat("BuyPageCardEvent.cs -- setPackageEvents() -- multiple buy page card event datas have been setup. This will not behave properly.");
		}
		maxCardEventBonus = getHighestCardEventLift(eventLiftData);
		hasMixedCardEventBonuses = findMixedCardEventBonuses(eventLiftData);
		cardEvent = findCardEvent(eventData);
		hasBeenSetup = true;
	}

	public string getHighestCardEventLift(string eventLiftData)
	{
		if (string.IsNullOrEmpty(eventLiftData))
		{
			Debug.LogWarning("No card event lift data");
			return "";
		}
		string[] eventLiftStrings = eventLiftData.Split(',');
		string maxLift = "";
		int maxValue = -1;
		for (int i = 0; i < eventLiftStrings.Length; i++)
		{
			string lift = eventLiftStrings[i];
			int value = 0;
			if (lift == "nothing" || lift == "0" || lift == "")
			{
				// Skip nothing AND the value 0 AND the empty string.
				continue;
			}
			else if (lift.Contains('X'))
			{
				string numberString = lift.Replace("X", "");
				try
				{
					// Parse out the multiplier, then convert to a percentage to make comparison easier.
					int.TryParse(numberString, out value);
					value = (value - 1) * 100;
				}
				catch (System.Exception e)
				{
					Debug.LogErrorFormat("PurchaseFeatureData.cs -- getHighestCardEventLift() -- Failed to parse integer from card lift value: {0}", lift);
					continue;
				}
			}
			else if (lift.Contains('%'))
			{
				// If its a percentage just remove the percent and it shoudl be a number.
				string numberString = lift.Replace("%", "");
				try
				{
					// Parse out the multiplier, then convert to a percentage to make comparison easier.
					int.TryParse(numberString, out value);
				}
				catch (System.Exception e)
				{
					Debug.LogErrorFormat("PurchaseFeatureData.cs -- getHighestCardEventLift() -- Failed to parse integer from card lift value: {0}", lift);
				}
			}
			else
			{
				// Otherwise, the PM forgot to enter a character after the value (ie '%' or 'X')
				// We should ignore this for the calculation of the max value.
				continue;
			}

			if (value > 0 && value > maxValue)
			{
				// MCC -- Making sure we dont count 0 as a valid value
				// If the int value is greater than our current one, then lets update
				maxLift = lift;
				maxValue = value;
			}
		}
		return maxLift;
	}

	public bool findMixedCardEventBonuses(string eventLiftData)
	{
		if (string.IsNullOrEmpty(eventLiftData))
		{
			Debug.LogWarning("No card event lift data");
			return false;
		}
		string[] eventLiftStrings = eventLiftData.Split(',');
		string nonNothingLift = "";
		for (int i = 0; i < eventLiftStrings.Length; i++)
		{
			string lift = eventLiftStrings[i];
			if (lift != "nothing")
			{
				if (!string.IsNullOrEmpty(nonNothingLift) && nonNothingLift != lift)
				{
					// If we have grabbed a nonNothing value, and it doesnt match, then we have mixed values
					return true;
				}
				nonNothingLift = lift;
			}
		}
		// If we made it this far, then we have all the same non-nothing values.
		return false;
	}

	public string getBuyCreditsHeaderTextMoreCards()
	{
		string locKey = getBuyCreditsHeaderLocalizationMoreCards(hasMixedCardEventBonuses, maxCardEventBonus);
		return Localize.text(locKey, maxCardEventBonus);
	}

	public string getBuyCreditsHeaderTextMoreRareCards()
	{
		string locKey = getBuyCreditsHeaderLocalizationMoreRareCards(hasMixedCardEventBonuses, maxCardEventBonus);
		return Localize.text(locKey, maxCardEventBonus);
	}

	public string getBuyCreditsHeaderLocalizationMoreCards(bool hasMixed, string maxEventBonus)
	{
		if (string.IsNullOrEmpty(maxEventBonus))
		{
			return "more_cards_header";
		}
		else if (hasMixed)
		{
			return "more_cards_header_up_to_{0}";
		}
		else
		{
			return "more_cards_header_{0}";
		}
	}

	public string getBuyCreditsHeaderLocalizationMoreRareCards(bool hasMixed, string maxEventBonus)
	{
		if (string.IsNullOrEmpty(maxEventBonus))
		{
			return "more_rare_cards_header";
		}
		else if (hasMixed)
		{
			return "more_rare_cards_header_up_to_{0}";
		}
		else
		{
			return "more_rare_cards_header_{0}";
		}
	}

	public CreditPackage.CreditEvent findCardEvent(string eventData)
	{
		string[] eventStrings = eventData.Split(',');
		CreditPackage.CreditEvent result = CreditPackage.CreditEvent.NOTHING;
		for (int i = 0; i < eventStrings.Length; i++)
		{
			string eventString = eventStrings[i];
			CreditPackage.CreditEvent creditEvent = stringToCreditEvent(eventString);
			if (creditEvent != CreditPackage.CreditEvent.NOTHING)
			{
				// More Rare Cards takes precedence as the current event if we have multiple defined,
				// so only overrite the result if it not already more_rare_cards.
				result = (result == CreditPackage.CreditEvent.MORE_RARE_CARDS) ? result : creditEvent;
			}
		}
		return result;
	}

	public List<KeyValuePair<CreditPackage.CreditEvent, string>> getEventsFromStrings(string eventData, string eventLiftData)
	{
	    List<KeyValuePair<CreditPackage.CreditEvent, string>> eventList = new List<KeyValuePair<CreditPackage.CreditEvent, string>>();
		string[] eventStrings = eventData.Split(',');
		string[] eventLiftStrings = eventLiftData.Split(',');
		if (eventLiftStrings.Length != eventStrings.Length)
		{
			Debug.LogWarning("Invalid event data on credit packages");
		}
		for (int i = 0; i < eventStrings.Length; ++i)
		{
			CreditPackage.CreditEvent newEvent = stringToCreditEvent(eventStrings.Length > i ? eventStrings[i] : "nothing");
			eventList.Add(new KeyValuePair<CreditPackage.CreditEvent, string>(newEvent, eventLiftStrings.Length > i ? eventLiftStrings[i] : ""));
		}

		return eventList;
	}

	private CreditPackage.CreditEvent stringToCreditEvent(string eventKey)
	{
		switch (eventKey)
		{
			case "more_cards":
				return CreditPackage.CreditEvent.MORE_CARDS;
			case "more_rare_cards":
				return CreditPackage.CreditEvent.MORE_RARE_CARDS;
			default:
				return CreditPackage.CreditEvent.NOTHING;
		}
	}
#region feature_base_overrides

	public override  bool isEnabled
	{
		get
		{
			return base.isEnabled &&
		    //timer active
			eventTimer.isActive &&
		    //no sale is active
		    !PurchaseFeatureData.isSaleActive &&
			// Requires collections to be on to function.
			Collectables.isActive() &&
			// If we dont have any valid events then the feature isnt really on.
			cardEvent != CreditPackage.CreditEvent.NOTHING;
		}
	}

	public override void drawGuts()
	{
		GUILayout.Label("Timer is active: " + eventTimer.isActive);
		GUILayout.Label("Collections is active: " + Collectables.isActive());
		GUILayout.Label("Card Event: " + cardEvent.ToString());
	}
	
	protected override void initializeWithData(JSON data)
	{
		// Grab the start/end times from live data.
		int startTime = Data.liveData.getInt(COLLECTIONS_CARDS_LIFT_START_TIME, 0);
		int endTime = Data.liveData.getInt(COLLECTIONS_CARDS_LIFT_END_TIME, 0);
		eventTimer = new GameTimerRange(startTime, endTime);
	}
#endregion
}
