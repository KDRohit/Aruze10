/*
	ModularPickPortal.cs
	
	Michael Cabral - 09/08/2016
	Scott Lepthien - 12/13/2016
	
	This is a fully modular pick portal intended to replace the old version the pick portal.
	
	This class takes care of the bare minimum a portal needs to do. All other functionality (Audio playing, animations, etc.)
	is accomplished via attached modules.
	
	NOTE TO FUTURE DEVS!
	--------------------
	
	Please do not add to this module unless it is absolutely warranted! 
	
	Before adding to this module perform the following steps:
	
	1) Consider if your desired effects can be accomplished with already existing modules.
	
	2) If not, consider if you can create a new module to handle what you're trying to do.

	3) If there is something you think absolutely should be in this file instead of in a module, ask a lead to see what they think.
	
	Portals are simple things; let's not over complicate things for ourselves.
*/

 using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class ModularPickPortal : ModularChallengeGame 
{
	// This is copied over from the original PickPortal module
	// TODO: We need to compleately replace the reliance on this and have some code to determine what kind of outcome the game
	//	has automatically.
	private enum ChallengeGameOutcomeType
	{
		WheelOutcome 					= 0,
		PickemOutcome 					= 1,
		NewBaseBonusOutcome 			= 2,
		WheelOutcomeWithChildOutcomes 	= 3,
		CrosswordOutcome 				= 4	// This is a special crossword outcome used for zynga04 and any related games.
	}
	
	// Copied from the original module, used internaly to keep track of the portal outcome type.
	public enum PortalTypeEnum
	{
		NONE 		= -1,
		PICKING 	= 0,
		FREESPINS 	= 1,
		CREDITS 	= 2
	}

	public static string getGroupIdForPortalType(PortalTypeEnum portalType)
	{
		switch (portalType)
		{
			case PortalTypeEnum.NONE:
				return "none";

			case PortalTypeEnum.PICKING:
				return PICKING_GAME_GROUP_ID;

			case PortalTypeEnum.FREESPINS:
				return FREESPINS_GROUP_ID;

			case PortalTypeEnum.CREDITS:
				return CREDITS_BONUS_GROUP_ID;

			default:
				Debug.LogError("ModularPickPortal.getGroupIdForPortalType() - Unhandled PortalTypeEnum! portalType = " + portalType);
				return "";
		}
	}

	private PortalTypeEnum portalType = PortalTypeEnum.NONE;

	private const string CREDITS_BONUS_NAME_PART = "_credit_bonus"; // name part used to identify if something is a credits a bonus
	private const string PORTAL_BONUS_NAME_PART = "_portal"; // name part used to identify if something is a portal bonus (these are obviously ignored by the portal code)
	
	// The following group ids will be used by PickingGamePortalModule so it will handle reveals correctly
	public const string PICKING_GAME_GROUP_ID = "picking";
	public const string FREESPINS_GROUP_ID = "freespins";
	public const string CREDITS_BONUS_GROUP_ID = "credits_bonus";

	protected override void Awake()
	{
		// ensure that we don't pop a bonus summary dialog
		BonusGamePresenter.instance.forceEarlyEnd = true;

		base.Awake();
	}

	// Searches for freespin games in an outcome and it's suboutcomes
	// This function is almost 100% accurate barring some weird edge conditions
	// One such edge condition might be a picking game which goes into a freespin game for instance (like in ted01)
	private SlotOutcome findFreespinsOutcome(SlotOutcome outcome)
	{
		string bonusGame = outcome.getBonusGame();
		if (!string.IsNullOrEmpty(bonusGame))
		{
			BonusGame bonusData = BonusGame.find(bonusGame);
			if (bonusData != null && bonusData.payTableType == BonusGame.PaytableTypeEnum.FREE_SPIN)
			{
				return outcome;
			}
		}

		// look for nested freespin games as well
		if (outcome.hasSubOutcomes())
		{
			ReadOnlyCollection<SlotOutcome> subOutcomes = outcome.getSubOutcomesReadOnly();
			for (int i = 0; i < subOutcomes.Count; i++)
			{
				SlotOutcome subOutcomeResult = findFreespinsOutcome(subOutcomes[i]);
				if (subOutcomeResult != null)
				{
					return subOutcomeResult;
				}
			}
		}
		return null;
	}

	// This function uses some fuzzy logic to figure out what its a challenge outcome or not, usually correct.
	private BaseBonusGameOutcome findChallengeOrCreditOutcome(SlotOutcome outcome, out SlotOutcome outcomeOut, out bool wasCreditOutcomeOut)
	{
		wasCreditOutcomeOut = false;
		outcomeOut = null;
		// Don't process top-level outcome
		if (outcome != SlotBaseGame.instance.outcome)
		{
			string bonusGameName = outcome.getBonusGame();
			BonusGame bonusData = null;
			if (!string.IsNullOrEmpty(bonusGameName))
			{
				bonusData = BonusGame.find(bonusGameName);
				if (bonusData != null)
				{
					switch (bonusData.payTableType)
					{
						case BonusGame.PaytableTypeEnum.PICKEM:
							outcomeOut = outcome;
							return new PickemOutcome(outcome);
						case BonusGame.PaytableTypeEnum.BASE_BONUS:
							outcomeOut = outcome;
							return new NewBaseBonusGameOutcome(outcome);
						case BonusGame.PaytableTypeEnum.CROSSWORD:
							outcomeOut = outcome;
							return new CrosswordOutcome(outcome);
						case BonusGame.PaytableTypeEnum.WHEEL:
							// HACK: there currently doesn't seem to be a way to determine whether a wheel outcome is actually a credit award.
							// For now we check the name for the common convention '_credit_bonus'. This is currently the standard in all our gmaes.
							// Though this is certainly not a robust solution.
							if (bonusGameName.Contains(CREDITS_BONUS_NAME_PART))
							{
								wasCreditOutcomeOut = true;
								outcomeOut = outcome;
								return new WheelOutcome(outcome);
							}
							// HACK: There currently doesn't seem to be a way to detect weather or not a bonus game is a portal.
							// For now we check the name for the common convention '_portal'.
							else if (!bonusGameName.Contains(PORTAL_BONUS_NAME_PART))
							{
								outcomeOut = outcome;
								return new WheelOutcome(outcome);
							}
							break;
						case BonusGame.PaytableTypeEnum.FREE_SPIN:
							// Found wrong kind of bonus game, these will be detected when we look for a free spin game, 
							// so we want to report that a challenge bonus game wasn't found
							return null;
						default:
							break;
					}
				}
			}
		}

		// If we didn't find something valid here, check the children to see if it is nested, for instance nested inside of the portal part of the outcome
		if (outcome.hasSubOutcomes())
		{
			ReadOnlyCollection<SlotOutcome> subOutcomes = outcome.getSubOutcomesReadOnly();
			for (int i = 0; i < subOutcomes.Count; i++)
			{
				BaseBonusGameOutcome subOutcomeResult = findChallengeOrCreditOutcome(subOutcomes[i], out outcomeOut, out wasCreditOutcomeOut);
				if (subOutcomeResult != null)
				{
					return subOutcomeResult;
				}
			}
		}

		return null;
	}

	// Builds an outcome that ModularChallengeGame will use so run this portal
	// This outcome will be represented as a NewBaseBonusGameOutcome which
	// uses groupId field to tell what type of portal reveal 
	private void createModularPortalOutcome()
	{
		List<BasePick> selected = new List<BasePick>();
		List<BasePick> unselected = new List<BasePick>();
		BasePick basePick;

		SlotOutcome currentOutcome = SlotBaseGame.instance.outcome;

		// cancel being a portal now that we're in it
		currentOutcome.isPortal = false;

		// Check for a freespins outcome
		// Search the current outcome for ANY freespins data, if it exits, we can safely assume we have a freespins game.
		SlotOutcome bonusOutcome = findFreespinsOutcome(currentOutcome);

		// If we have an outcome at this point, we def. have a freespins game
		if (bonusOutcome != null)
		{
			string bonusGameName = bonusOutcome.getBonusGame();
			BonusGame thisBonusGame = BonusGame.find(bonusGameName);
			BonusGameManager.instance.summaryScreenGameName = bonusGameName;
			BonusGameManager.instance.isGiftable = thisBonusGame.gift;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusOutcome);
			currentOutcome.isGifting = true;
			portalType = PortalTypeEnum.FREESPINS;
			basePick = new BasePick();
			basePick.groupId = FREESPINS_GROUP_ID;
			selected.Add(basePick);
		}

		// Check for a challenge game (which include credits bonuses)
		bonusOutcome = null;
		bool wasCreditOutcome = false;
		BaseBonusGameOutcome challengeOutcome = findChallengeOrCreditOutcome(currentOutcome, out bonusOutcome, out wasCreditOutcome);
		if (challengeOutcome != null)
		{
			// If it wasn't a credit outcome, we assume it's some kind of challenge outcoem
			if (!wasCreditOutcome)
			{
				string bonusGameName = bonusOutcome.getBonusGame();
				BonusGameManager.instance.summaryScreenGameName = bonusGameName;
				BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = challengeOutcome;
				currentOutcome.isChallenge = true;
				portalType = PortalTypeEnum.PICKING;
				basePick = new BasePick();
				basePick.groupId = PICKING_GAME_GROUP_ID;
				selected.Add(basePick);
			}
			else
			{
				// this is a credits_bonus
				currentOutcome.winAmount = (challengeOutcome as WheelOutcome).getNextEntry().credits;
				currentOutcome.isCredit = true;
				BonusGamePresenter.instance.currentPayout = SlotBaseGame.instance.getCreditBonusValue();
				portalType = PortalTypeEnum.CREDITS;
				basePick = new BasePick();
				basePick.credits = SlotBaseGame.instance.getCreditBonusValue();
				basePick.groupId = CREDITS_BONUS_GROUP_ID;
				selected.Add(basePick);
			}
		}

		// Make sure we only have one selected element since you only pick one thing for a portal
		// this will hopefully help detect any future issue that could arise with strange things happening
		// if different types of bonus games are nested and not detected correctly
		if (selected.Count > 1)
		{
			Debug.LogError("ModularPickPortal.createModularPortalOutcome() - More than one bonus was detected as selected for the portal, this shouldn't happen!");
		}

		// Randomize a list of portal type objects that will reveal
		basePick = new BasePick();
		basePick.groupId = PICKING_GAME_GROUP_ID;
		unselected.Add(basePick);

		basePick = new BasePick();
		basePick.groupId = FREESPINS_GROUP_ID;
		unselected.Add(basePick);

		if (hasACreditsBonus())
		{
			basePick = new BasePick();
			basePick.groupId = CREDITS_BONUS_GROUP_ID;
			basePick.credits = SlotBaseGame.instance.getCreditMadeupValue();
			unselected.Add(basePick);
		}

		// Add in any extra "filler" reveals based on the length of the pickButtons list and the entrys already in the unselected List
		// This is in case there are more buttons than reveal types (i.e. more than one reveal per type)
		// Example of this is Zynga04 - Words with friends
		List<BasePick> gamePortalTypes = new List<BasePick>(unselected);
		ModularChallengeGameRound currentRound = getCurrentRound();
		ModularPickingGameVariant currentVariant = currentRound.getCurrentVariant() as ModularPickingGameVariant;

		int remainingPicks = 0;
		if (currentVariant == null)
		{
			Debug.LogError("ModularPickPortal.buildPortalOutcomeForModularChallengeGame() - Can't determine variant so don't know how many buttons there are!");
		}
		else
		{
			remainingPicks = currentVariant.pickAnchors.Count - unselected.Count;
		}

		if (remainingPicks > 0)
		{
			for (int i = 0; i < remainingPicks; ++i)
			{
				BasePick randomPortalReveal = gamePortalTypes[Random.Range(0, gamePortalTypes.Count)];
				basePick = new BasePick();
				basePick.groupId = randomPortalReveal.groupId;

				// make another fake credits value if this is a credits reveal so they don't all say the same thing
				if (basePick.groupId == CREDITS_BONUS_GROUP_ID)
				{
					basePick.credits = SlotBaseGame.instance.getCreditMadeupValue();
				}

				unselected.Add(basePick);
			}
		}

		// remove an entry of the type we are going to reveal
		int removeAtIndex = -1;
		string selectedGroupId = ModularPickPortal.getGroupIdForPortalType(portalType);
		for (int i = 0; i < unselected.Count; i++)
		{
			if (unselected[i].groupId == selectedGroupId)
			{
				removeAtIndex = i;
				break;
			}
		}

		if (removeAtIndex != -1)
		{
			unselected.RemoveAt(removeAtIndex);
		}

		// shuffle the unselected entries
		CommonDataStructures.shuffleList<BasePick>(unselected);

		// Create the outcome we will use in the ModularPickingGameVariant
		NewBaseBonusGameOutcome portalModularNewBaseBonusOutcome = new NewBaseBonusGameOutcome();
		portalModularNewBaseBonusOutcome.roundPicks.Add(0, new RoundPicks("", selected, unselected, null));
		ModularChallengeGameOutcome portalModularBonusOutcome = new ModularChallengeGameOutcome(portalModularNewBaseBonusOutcome);
		List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
		variantOutcomeList.Add(portalModularBonusOutcome);
		addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
	}

	// Check if this game has a credits bonus by looking at the bonus paytables it has
	private bool hasACreditsBonus()
	{
		List<string> bonusGameDataKeys = ReelGame.activeGame.slotGameData.bonusGameDataKeys;
		for (int i = 0; i < bonusGameDataKeys.Count; i++)
		{
			if (bonusGameDataKeys[i].Contains(CREDITS_BONUS_NAME_PART))
			{
				return true;
			}
		}

		return false;
	}

	// Called when it's time to initialize the module
	public override void init()
	{
		createModularPortalOutcome();
		base.init();
	}

	// Called when it's time to end the portal
	protected override void endGame()
	{
		StartCoroutine(endGameCoroutine());
	}

	// Coroutine version of endGame so we can handle a transition that might occur as this game ends and it spawns into the next bonus
	private IEnumerator endGameCoroutine()
	{
		if (portalType == PortalTypeEnum.CREDITS)
		{
			// we want to send the player back to the base game, since there isn't a bonus game for a credits win
			BonusGamePresenter.instance.isReturningToBaseGameWhenDone = true;
		}
		else
		{
			// the player will be continuing onto another bonus game from here
			BonusGamePresenter.instance.isReturningToBaseGameWhenDone = false;
		}

		base.endGame();

		// Begin the actual bonus game at this point. Mostly copied over from the old module.
		if (portalType == PortalTypeEnum.CREDITS)
		{
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.goIntoBonus();
			}
			else
			{
				Debug.LogError("ModularPickPortal.endGame() - There is no SlotBaseGame instance, can't start bonus game...");
			}
		}
		else
		{
			// If our reel game isn't active then don't try to grab any modules from it
			// If in the future for some reason you need one to trigger you'll have to look into 
			// using RoutineRunner in the modules so they run even if the object they are attached to is disabled
			if (ReelGame.activeGame.gameObject.activeSelf) 
			{
				// Check if a transition needs to be triggered
				for (int i = 0; i < ReelGame.activeGame.cachedAttachedSlotModules.Count; i++)
				{
					SlotModule module = ReelGame.activeGame.cachedAttachedSlotModules[i];

					// handle the pre bonus created modules, needed for some transitions
					if (module.needsToExecuteOnPreBonusGameCreated())
					{
						yield return StartCoroutine(module.executeOnPreBonusGameCreated());
					}
				}
			}

			if (portalType == PortalTypeEnum.PICKING)
			{
				BonusGameManager.instance.create(BonusGameType.CHALLENGE);
			}
			else
			{
				BonusGameManager.instance.create(BonusGameType.GIFTING);
			}

			BonusGameManager.instance.show();
		}
	}
}
