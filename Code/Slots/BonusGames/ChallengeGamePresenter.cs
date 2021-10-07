using Com.Scheduler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Base class for challenge game presentation
 */
public class ChallengeGamePresenter :TICoroutineMonoBehaviour, IResetGame
{
    
    protected readonly struct BonusEventIdentifier
    {
        public readonly string eventName;
        
        public readonly int gameNum;

        public BonusEventIdentifier(string pEventName, int pGameNum)
        {
            eventName = pEventName;
            gameNum = pGameNum;
        }
    }
    
    public GameObject gameScreen;
    
    [HideInInspector] public bool isProgressive = false; // Bonus Summary screens need to know if a game is progressive.
    [HideInInspector] public bool isGameActive = false; // Tracks if the game is active or not
    [HideInInspector] public long currentPayout = 0;
    [HideInInspector] public string bonusGameName = "";
    [HideInInspector] public string paytableSetId = "";
    
    protected bool canEnd = false;	// Bonus Summary screens can only show once per game. Here is where we force that condition.
    
    public bool useMultiplier = true;
    public bool forceEarlyEnd = false;
    public bool isAutoPlayingInitMusic = true;
    public bool destroyOnEnd = true; // Most games aren't reused, they are played and then destroyed, but in some cases for say a feature of the base game we might just want to reuse the same instance over and over

    // When you finish the bonus game, it takes a few frames to destroy it.
    // If anything lingers on top of the base game (like labels),
    // then enable the deactivation failsafe to hide them.
    [Tooltip("If anything lingers on top of the base game (like labels), then enable the deactivation failsafe to hide them")]
    public bool useDeactivationFailsafe = false;
    public bool hideNameInBonusSummaryDialog = false;
    
    
    // There may be multiple bonus game identifiers, like in gowtw01, so we store them in a list. These identifiers must be ordered by gameNum.
    private static List<BonusEventIdentifier> bonusEventIdentifiers = new List<BonusEventIdentifier>();
    
    protected virtual void welcomeButtonClicked()
    {
	    gameScreen.SetActive(true);
	    isGameActive = true;
    }

    protected virtual bool hasLightSet()
    {
	    return ChallengeGame.instance != null && ChallengeGame.instance.hasItsOwnLightSet;
    }
    
    // Removes the first (next) item from the list and returns it to the caller
    public static string NextBonusGameIdentifier()
    {
	    if(bonusEventIdentifiers.Count == 0)
		    return "";

	    string evtName = bonusEventIdentifiers[0].eventName;
	    bonusEventIdentifiers.RemoveAt(0);
	    return evtName;
    }
	
	
    // Use this to querry weather or not the list has any identifiers left
    public static bool HasBonusGameIdentifier()
    {
	    return bonusEventIdentifiers.Count > 0 ? true : false;
    }
    
    // Accessor function used to add a bonus game identifier
    public static void AddBonusEventIdentifier(string name, int gameNum = 0)
    {
	    // We use a List and a sort because there is no guarantee what order the data is going to come in via JSON.
	    //	The only way to guarantee an order is to sort on insert based on gameNum.
	    bonusEventIdentifiers.Add(new BonusEventIdentifier(name, gameNum));
	    bonusEventIdentifiers.Sort((param1, param2) => param1.gameNum.CompareTo(param2.gameNum));
    }
    
    public virtual bool gameEnded()
	{
		if (!canEnd)
		{
			return false;
		}

		if (Dialog.instance != null)
		{
			if (hasLightSet())
			{
				Dialog.instance.keyLight.SetActive(true);
			}
		}
		
		canEnd = false;

		BonusGamePresenter.portalPayout = 0;
		// Add to the final payout instead of just setting it, so queued bonuses get added together
		BonusGameManager.instance.finalPayout += currentPayout;
		BonusGameManager.instance.currentGameFinalPayout = currentPayout;

		/*
		if (GameState.giftedBonus != null && GiftedSpinsVipMultiplier.isActive)
		{
			System.Decimal currentPayoutDecimal = System.Convert.ToDecimal(BonusGameManager.instance.finalPayout);
			currentPayoutDecimal *= System.Convert.ToDecimal(GiftedSpinsVipMultiplier.playerMultiplier);
			// If this is a gifted free bonus, then multiply the payout by the vip multiplier.
			Debug.LogErrorFormat("BonusGamePresenter.cs -- gameEnded -- useMultiplier is : {0}", useMultiplier);
			BonusGameManager.instance.finalPayout = System.Convert.ToInt64(System.Decimal.Round(currentPayoutDecimal));
			
		}
		*/
		if (useMultiplier)
		{
			BonusGameManager.instance.finalPayout *= BonusGameManager.instance.currentMultiplier;
			BonusGameManager.instance.currentGameFinalPayout *= BonusGameManager.instance.currentMultiplier;
		}

		bool usingCustomSummaryDialog = false;

		// if it's a gifted challenge, don't show the Bonus summary, go strait to the challenge summary.
		// MCC -- We want to show the bonus summary screen with the gifted free spins if we are in the gifted vip multiplier experiment so they see the surfacing.
		if (BonusGameManager.instance.currentGameType == BonusGameType.CHALLENGE &&
			GameState.giftedBonus != null)
		{
			if (ChallengeGame.instance != null && ChallengeGame.instance is ModularChallengeGame)
			{
				if ((ChallengeGame.instance as ModularChallengeGame).getCurrentRound() != null && (ChallengeGame.instance as ModularChallengeGame).getCurrentRound().getCurrentVariant() != null)
				{
					for (int i = 0; i < (ChallengeGame.instance as ModularChallengeGame).getCurrentRound().getCurrentVariant().cachedAttachedModules.Count; i++)
					{
						if ((ChallengeGame.instance as ModularChallengeGame).getCurrentRound().getCurrentVariant().cachedAttachedModules[i].needsToShowCustomBonusSummaryDialog())
						{
							usingCustomSummaryDialog = true;
							(ChallengeGame.instance as ModularChallengeGame).getCurrentRound().getCurrentVariant().cachedAttachedModules[i].createCustomSummaryScreenDialog(summaryClosed);
						}
					}
				}
			}
			if (!usingCustomSummaryDialog)
			{
				summaryClosed();
			}
		}
		else if (!forceEarlyEnd)
		{
			//Check for custom dialog
			if (FreeSpinGame.instance != null)
			{
				foreach (SlotModule module in FreeSpinGame.instance.cachedAttachedSlotModules)
				{
					if (module.needsToCreateCustomSummaryScreenDialog())
					{
						usingCustomSummaryDialog = true;
						module.createCustomSummaryScreenDialog(summaryClosed);
					}
				}
			}

			if (!usingCustomSummaryDialog)
			{
				BonusSummary.handlePresentation(
					Dict.create(
						D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { summaryClosed(); })
					)
				);

			}
		}
		
		isGameActive = false;
		
		if (forceEarlyEnd)
		{
			executeModulesThenFinalCleanup();
		}

		return true;
	}
    
    /// The summary dialog has been closed.
	public virtual void summaryClosed()
	{
		executeModulesThenFinalCleanup();
	}
    
    protected void executeModulesThenFinalCleanup()
    {
	    StartCoroutine(executeModulesThenFinalCleanupCoroutine());
    }

    protected virtual IEnumerator executeModulesThenFinalCleanupCoroutine()
    {
	    if (ChallengeGame.instance != null)
	    {
		    yield return StartCoroutine(ChallengeGame.instance.handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules());
	    }
	    
	    // now that the bonuses have had a chance to do stuff before we kill them, perform finalCleanup!
	    finalCleanup();
    }
    
    /// Do all cleanup and go back to whatever is appropriate.
	public virtual void finalCleanup()
	{
		
		if (useDeactivationFailsafe)
		{
			gameObject.SetActive(false);
		}
		
		if (destroyOnEnd)
		{
			// Hide the gameobject before we destroy it to hopefully stop NGUI from
			// rendering ghost frames of some panels
			gameObject.SetActive(false);
			GameObject.Destroy(gameObject);
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

    public virtual void init(bool isCheckingReelGameCarryOverValue)
    {
	    currentPayout = 0;
	    isProgressive = false;
	    canEnd = true;
	    
	    if (Dialog.instance != null)
	    {
		    if (ChallengeGame.instance != null && ChallengeGame.instance.hasItsOwnLightSet)
		    {
			    Dialog.instance.keyLight.SetActive(false);
		    }
	    }
		
	    Bugsnag.LeaveBreadcrumb("Starting a bonus game");
	    
	    // turn the welcome screen off until dev is completed. 
	    /*
	    gameScreen.SetActive(false);
	    summaryScreen.SetActive(false);
	    welcomeScreen.SetActive(true);
	    */
	    // Press the welcome button instead. 
	    welcomeButtonClicked();
    }

    
    public static void resetStaticClassData()
    {
        bonusEventIdentifiers.Clear();
    }
}
