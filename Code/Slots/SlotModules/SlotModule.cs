using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * SlotModule.cs
 * author: Nick Reynolds
 * SlotModule is the base class for all modules that get attached to Slot Games.
 * 
 * Modules are a way of attaching features to Slot Games without having to extend
 * SlotBaseGame or another class. Most, if not all, modules should be able to be
 * attached to either a base game or a free spins game and work correctly (assuming
 * the data is correct). 
 * 
 * Modules are accessed at different points in the playing of a Slots Game. Examples given below
 * 
 * executeOnReelsStoppedCallback() 	- module logic is executed at the very beginning of ReelGame.reelsStoppedCallback()
 * 									  note that many games extend SlotBaseGame/FreeSpinGame and override the implementation of executeOnReelsStoppedCallback()
 * 									  Modules that execute at the reels stopped callback will only execute when we reach the ReelGame execution level
 * 
 * executeOnStartPayoutRollupEnd() 	- module logic is executed near the end of the OutcomeDisplayController.startPayoutRollup(), essentially at the same location
 * 									  where a game's BonusPoolCoroutine is started (if the game and outcome have these set). Consider replacing use of BonusPoolCoroutine
 * 									  with modules that execute on this location.
 *
 * executeOnPaylineDisplay() 		- module logic is executed when a payline is shown inside of PaylineOutcomeDisplayModule::playOutcome()
 *
 * executeOnPaylineHide() 			- module logic is executed when a payline is hidden inside of PaylineOutcomeDisplayModule::displayFinish()
 * 
 * KEEP ABOVE SECTION UPDATED AS MORE ENTRY/EXECUTION POINTS FOR MODULES ARE ADDED
 * 
 * Modules will often have more than one execution point. This is ok, but try to limit it as much as possible. Ideally a module is never responsible for 
 * more than 1 feature. What exactly qualifies as a "feature" is up to the developer, but the smaller each module is, the easier the system will be to maintain.
 *
 */ 
public class SlotModule : TICoroutineMonoBehaviour 
{
	// Feel free to add in the other hooks to this event list as they are
	// needed for event type modules that want to allow a module to respond
	// to multiple hooks in the same module
	public enum SlotModuleEventType
	{
		OnSlotGameStartedNoCoroutine = 1,
		OnSlotGameStarted = 2,
		OnPrespin = 3,
		OnReevaluationPreSpin = 4,
		OnReelsStoppedCallback = 5,
		OnReevaluationReelsStoppedCallback = 6,
		OnFreespinGameEnd = 7
	}
	
	// Class designed in order to allow for the dynamic sorting when displayed in Editor
	// of SlotModuleEventType for SlotModuleEventHandler.
	[System.Serializable]
	public class SortedSlotModuleEventType
	{
		public SlotModuleEventType slotEvent;
	}

	// This is intended to be used as the base class for data classes made to be
	// used with BaseOnSlotEventModule.  See InitAudioCollectionOnEventModule for
	// an example usage.
	[System.Serializable]
	public abstract class SlotModuleEventHandler
	{
		public delegate void OnEventDelegate();
		public delegate IEnumerator OnEventCoroutineDelegate();
		
		[Tooltip("The events which this data will react to")]
		public SortedSlotModuleEventType[] eventList;

		[System.NonSerialized] public SlotModule slotModule;
		[System.NonSerialized] public ReelGame reelGame;
		[System.NonSerialized] public OnEventDelegate onEventDelegate;
		[System.NonSerialized] public bool isOnEventCoroutineDelegateBlocking = true; // set this in setOnEventDelegates() if you don't want onEventCoroutineDelegate to block
		[System.NonSerialized] public OnEventCoroutineDelegate onEventCoroutineDelegate;
		
		// Sets the SlotModule who this handler is attached to
		public void setParentSlotModule(SlotModule parentSlotModule)
		{
			slotModule = parentSlotModule;
			reelGame = parentSlotModule.reelGame;
		}
		
		// This function must be defined in the derived classes in order to set
		// onEventDelegate and/or onEventCoroutineDelegate which will trigger 
		// when a SlotModule event matching what is in eventList occurs
		public abstract void setOnEventDelegates();
	}
	
	protected ReelGame reelGame;
	
	[SerializeField] protected bool freeSpinGameRequired;

	public virtual void Awake()
	{
		// Avoid a getcomponent call if a subclass has already assigned reelGame
		reelGame = reelGame ? reelGame : GetComponent<ReelGame>();
		
		if (reelGame == null)
		{
			Debug.LogError("No ReelGame component found for " + this.GetType().Name + " - Destroying script.");
			Destroy(this);
		}
		else
		{
			if (freeSpinGameRequired && !reelGame.isFreeSpinGame()) 
			{
				Debug.LogError("No FreeSpinGame type found for " + this.GetType().Name + " - Destroying script.");
				Destroy(this);
			}
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		// fill this in if you need to do something when the script becomes enabled again
	}

	protected virtual void OnDestroy()
	{
		// Need to remove the SlotModule from the reelGame it is attached to
		if (reelGame != null)
		{
			reelGame.removeModuleFromCachedAttachedSlotModules(this);
		}
	}

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public virtual bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return false;
	}

	public virtual void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{

	}

	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public virtual bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return false;
	}
	
	public virtual IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield break;
	}

	// executeOnReelsSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public virtual bool needsToExecuteOnReelsSpinning()
	{
		return false;
	}
	
	public virtual IEnumerator executeOnReelsSpinning()
	{
		yield break;
	}

	// getDelaySpecificReelStop() section
	public virtual float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return 0.0f;
	}

	// aruze02 requires custom rollup delay override.
	public virtual bool needsToOverrideRollupDelay()
	{
		return false;
	}

	public virtual float getRollupDelay()
	{
		return 0.0f;
	}

	// similar to the section above but it would replace the reel timing value entirely for a single stop index
	// (Note that might include more than one reel, thus why a list of reels is being passed), rather than adding to it
	public virtual bool shouldReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return false;
	}
	public virtual float getReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return 0.0f;
	}

	// executeOnSpecificReelStopping() section
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public virtual bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return false;
	}
	
	public virtual void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{

	}

	// executeOnSpecificReelStopping() section
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public virtual bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return false;
	}
	
	public virtual IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		yield break;
	}

	// Used if the feature anticipation name is being determined in a module instead of the reelGame
	public virtual bool needsToGetFeatureAnicipationNameFromModule()
	{
		return false;
	}

	public virtual string getFeatureAnticipationNameFromModule()
	{
		return "";
	}

	// Used to handle playing the reel anticipation in special circumstances. First used in Batman01
	public virtual bool needsToPlayReelAnticipationEffectFromModule(SlotReel stoppedReel)
	{
		return false;
	}
	
	public virtual void playReelAnticipationEffectFromModule(SlotReel stoppedReel, Dictionary<int, Dictionary<int, Dictionary<string, int>>> anticipationTriggers)
	{
		
	}
	
	// Used to handle hiding the reel anticipation in special circumstances. First used in Batman01
	public virtual bool needsToHideReelAnticipationEffectFromModule(SpinReel stoppedReel)
	{
		return false;
	}
	
	public virtual IEnumerator hideReelAnticipationEffectFromModule(SpinReel stoppedReel)
	{
		yield break;
	}
	
	// Used if the feature anticipation name is being determined in a module instead of the reelGame
	public virtual bool needsToPlayReelAnticipationSoundFromModule()
	{
		return false;
	}

	public virtual void playReelAnticipationSoundFromModule()
	{
		
	}

	// executeOnPlayAnticipationSound() section
	// Functions here are executed in SpinReel where SlotEngine.playAnticipationSound is called
	public virtual bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return false;
	}
	
	public virtual IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		yield break;
	}

	//This is used to bypass the default playAnticipationSound() behavior in SlotEngine.cs  Any modules that return true can then use the needsToExecuteOnPlayAnticipationSound and executeOnPlayAnticipationSound
	//Example: ScatterSymbolLandingSoundModule
	public virtual bool isOverridingAnticipationSounds(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return false;
	}

	public virtual bool needsToExecuteOnSetSymbolPosition(SlotReel reel, SlotSymbol symbol, float verticalSpacing)
	{
		return false;
	}

	public virtual void executeOnSetSymbolPosition(SlotReel reel, SlotSymbol symbol, float verticalSpacing)
	{
		
	}

	// executeOnSpecificReelStopping() section
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public virtual bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		return false;
	}
	
	public virtual void executeOnSpinEnding(SlotReel stoppedReel)
	{
		
	}


	// executePreReelsStopSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public virtual bool needsToExecutePreReelsStopSpinning()
	{
		return false;
	}
	
	public virtual IEnumerator executePreReelsStopSpinning()
	{
		yield break;
	}

	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public virtual bool needsToExecuteOnPreSpin()
	{
		return false;
	}

	public virtual IEnumerator executeOnPreSpin()
	{
		yield break;
	}
	// executeOnPreSpinNoCoroutine() section
	// Functions here are executed during the startSpinCoroutine but do not spawn a coroutine
	public virtual bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return false;
	}

	public virtual void executeOnPreSpinNoCoroutine()
	{
	}

	// executeOnShowSlotBaseGame() section
	// Functions here are executed when the base game is restored after being hidden by events like BigWin effect or full screen dialogs,
	public virtual bool needsToExecuteOnShowSlotBaseGame()
	{
		return false;
	}

	public virtual void executeOnShowSlotBaseGame()
	{

	}

	// executeOnBigWinEnd() section
	// Functions here are executed after the big win has been removed from the screen.
	public virtual bool needsToExecuteOnBigWinEnd()
	{
		return false;
	}
	
	public virtual void executeOnBigWinEnd()
	{
		
	}


	// executeOnPreBigWin() section
	// Functions here are executed before the bigwin is created.
	public virtual bool needsToExecuteOnPreBigWin()
	{
		return false;
	}

	public virtual IEnumerator executeOnPreBigWin()
	{
		yield break;
	}

	// overrideBigWinSounds() section
	// Functions here are executed in Slotbasegame.onBigWinNotification
	public virtual bool needsToOverrideBigWinSounds()
	{
		return false;
	}

	public virtual void overrideBigWinSounds()
	{

	}
	

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public virtual bool needsToExecuteOnReelsStoppedCallback()
	{
		return false;
	}

	public virtual IEnumerator executeOnReelsStoppedCallback()
	{
		yield break;
	}

	// special function which hopefully shouldn't be used by a lot of modules
	// but this will allow for the game to not continue when the reels stop during
	// special features.  This is required for the rhw01 type of game with the 
	// SC feature which does respins which shouldn't allow the game to unlock
	// even on the last spin since the game should unlock when it returns to the
	// normal game state.
	public virtual bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return true;
	}

	// Very similar to onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	// however this blocks the spin being marked complete when returning from a bonus
	// game.  Can be useful for features like got01 where the feature can trigger
	// multiple bonuses that all need to resolve before the spin is actually complete
	public virtual bool isAllowingShowNonBonusOutcomesToSetIsSpinComplete()
	{
		return true;
	}

	// executeOnReelsSlidingCallback() section
	// functions in this section are called when a sliding slot games reels slide.
	public virtual bool needsToExecuteOnReelsSlidingCallback()
	{
		return false;
	}

	public virtual IEnumerator executeOnReelsSlidingCallback()
	{
		yield break;
	}

	// executeOnReelsSlidingCallback() section
	// functions in this section are called when a sliding slot games reels slide.
	public virtual bool needsToExecuteOnReelsSlidingEnded()
	{
		return false;
	}

	public virtual IEnumerator executeOnReelsSlidingEnded()
	{
		yield break;
	}

	// executeOnChangeSymbolToSticky() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public virtual bool needsToExecuteOnChangeSymbolToSticky()
	{
		return false;
	}

	public virtual IEnumerator executeOnChangeSymbolToSticky(SlotSymbol symbol, string name)
	{
		yield break;
	}

	// executeAfterSymbolplit() section
	// functions in this section are accessed by SlotSymbol.split()
	public virtual bool needsToExecuteAfterSymbolSplit()
	{
		return false;
	}
	
	public virtual void executeAfterSymbolSplit(SlotSymbol splittableSymbol)
	{

	}

	// executeAfterSymbolSetup() secion
	// Functions in this section are called once a symbol has been setup.
	public virtual bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return false;
	}
	
	public virtual void executeAfterSymbolSetup(SlotSymbol symbol)
	{

	}

	// executeAfterPaylinesCallback() section
	// functions in this section are called after the paylines are shown.
	public virtual bool needsToExecuteAfterPaylines()
	{
		return false;
	}

	public virtual IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		yield return null;
	}

	// executeOnPaylinesPayoutRollup() section
	// functions in this section are accessed by SlotbaseGame/FreeSpinGame.doReelsStopped() and are called on payout rollup,
	// before executeOnStartPayoutRollup
	public virtual bool needsToExecuteOnPaylinesPayoutRollup()
	{
		return false;
	}

	public virtual void executeOnPaylinesPayoutRollup(bool winsShown, TICoroutine rollupCoroutine = null)
	{
		
	}

	// executeOnReevaluationPreSpin() section
	// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
	// and triggers before the reels begin spinning
	public virtual bool needsToExecuteOnReevaluationPreSpin()
	{
		return false;
	}

	public virtual IEnumerator executeOnReevaluationPreSpin()
	{
		yield break;
	}

	// executeOnReevaluationSpinStart() section
	// functions in this section are accessed by ReelGame.startNextReevaluationSpin()
	public virtual bool needsToExecuteOnReevaluationSpinStart()
	{
		return false;
	}

	public virtual IEnumerator executeOnReevaluationSpinStart()
	{
		yield break;
	}

	// executeOnReevaluationReelsSpinning() section
	// Handles what executePreReelsStopSpinning() does, but during the reevaulation spins
	// Called from ReelGame.startNextReevaluationSpin()
	public virtual bool needsToExecuteOnReevaluationPreReelsStopSpinning()
	{
		return false;
	}

	public virtual IEnumerator executeOnReevaluationPreReelsStopSpinning()
	{
		yield break;
	}


	// executeOnReevaluationReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public virtual bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return false;
	}

	public virtual IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		yield break;
	}

	// executeOnStartPayoutRollup() section
	// functions in this section are accessed by OutcomeDisplayController.startPayoutRollup()
	public virtual bool needsToExecuteOnStartPayoutRollup(long bonusPayout, long basePayout)
	{
		return false;
	}

	public virtual IEnumerator executeOnStartPayoutRollup(long bonusPayout, long basePayout)
	{
		yield break;
	}

	// executeOnStartPayoutRollupEnd() section
	// functions in this section are accessed by OutcomeDisplayController.startPayoutRollup()
	public virtual bool needsToExecuteOnStartPayoutRollupEnd(long bonusPayout, long basePayout)
	{
		return false;
	}

	public virtual IEnumerator executeOnStartPayoutRollupEnd(long bonusPayout, long basePayout)
	{
		yield break;
	}

	// executeOnPaylineDisplay() section
	// functions in this section are accessed by ReelGame.onPaylineDisplayed() and are called on each payline displayed
	public virtual bool needsToExecuteOnPaylineDisplay()
	{
		return false;
	}

	public virtual IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		yield break;
	}

	// executeOnPaylineHide() section
	// function in this section are accesed by ReelGame.onPaylineHidden()
	public virtual bool needsToExecuteOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		return false;
	}

	public virtual IEnumerator executeOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		yield break;
	}

	// executeOnFreespinGameEnd() section
	// functions in this section are accessed by FreeSpinGame.gameEnded()
	public virtual bool needsToExecuteOnFreespinGameEnd()
	{
		return false;
	}
	
	public virtual IEnumerator executeOnFreespinGameEnd()
	{
		yield break;
	}

	public virtual bool needsToCreateCustomSummaryScreenDialog()
	{
		return false;
	}

	public virtual void createCustomSummaryScreenDialog(GenericDelegate answerDelegate)
	{
		
	}

	// executeOnReleaseSymbolInstance() section
	// functions in this section are accessed by ReelGame.releaseSymbolInstance
	public virtual bool needsToExecuteOnReleaseSymbolInstance()
	{
		return false;
	}

	public virtual void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
	}

	// executeOnPloppingFinished() section
	// functions in this section are accessed by 
	public virtual bool needsToExecuteOnPloppingFinished()
	{
		return false;
	}

	public virtual IEnumerator executeOnPloppingFinished(JSON currentTumbleOutcome, bool useTumble = false)
	{
		yield break;
	}

	// executeOnBonusHitsIncrement() section
	// functions here are called by the SpinReel incrementBonusHits() function
	// currently only used by gwtw01 for it's funky bonus symbol sounds
	public virtual bool needsToExecuteOnBonusHitsIncrement()
	{
		return false;
	}

	public virtual void executeOnBonusHitsIncrement(int reelID)
	{
	}

	// executeOnBonusGameEnded() section
	// functions here are called by the SlotBaseGame onBonusGameEnded() function
	// usually used for reseting transition stuff
	public virtual bool needsToExecuteOnBonusGameEnded()
	{
		return false;
	}

	public virtual IEnumerator executeOnBonusGameEnded()
	{
		yield break;
	}
	
	// executeOnDoSpecialOnBonusGameEnd() section
	// functions here are called by the SlotBaseGame doSpecialOnBonusGameEnd() function
	// usually used for reseting base game stuff after the base game is re-enabled when
	// coming back from a bonus
	// NOTE : Doesn't block because this function is not executed in a blocking way
	// so make sure what you do with this hook doesn't require blocking
	public virtual bool needsToExecuteOnDoSpecialOnBonusGameEnd()
	{
		return false;
	}

	public virtual void executeOnDoSpecialOnBonusGameEnd()
	{
	}

	//Used if theres a transition back to the basegame.
	//This prevents paylines and outcome symbols from displaying over a transition
	public virtual bool needsToLetModuleTransitionBeforePaylines()
	{
		return false;
	}

	//Used if the game's portal has a special sequence of sounds to play that aren't already supported by PrefabPortalScript.cs
	public virtual bool needsToLetModulePlayPortalRevealSounds()
	{
		return false;
	}

	public virtual IEnumerator executeOnPlayPortalRevealSounds(SlotOutcome outcome)
	{
		yield break;
	}

	//Used if the game's portal has a special sequence of sounds to play that aren't already supported by PrefabPortalScript.cs
	public virtual bool needsToLetModulePlayPortalTransitionSounds()
	{
		return false;
	}

	public virtual IEnumerator executeOnPlayPortalTransitionSounds(SlotOutcome outcome)
	{
		yield break;
	}

	// executeOnPreBonusGameCreated() section
	// functions here are called by the SLotBaseGame reelGameReelsStoppedCoroutine() function
	// used to handle delays (like transitions) before the bonus game is created, otherwise you will end up with both games showing up at the same time
	public virtual bool needsToExecuteOnPreBonusGameCreated()
	{
		return false;
	}

	public virtual IEnumerator executeOnPreBonusGameCreated()
	{
		yield break;
	}

	public virtual bool needsToUseACustomSpinPanelSizer()
	{
		return false;
	}

	public virtual BoxCollider2D getCustomSpinPanelSizer() 
	{
		return null;
	}
	
	public virtual bool needsToLetModuleCreateBonusGame()
	{
		return false;
	}

	public virtual bool needsToExecuteOnBeginRollback(SlotReel reel)
	{
		return false;
	}

	public virtual IEnumerator executeOnBeginRollback(SlotReel reel)
	{
		yield break;
	}
	// executeOnReelEndRollback() section
	// functions here are called by the SpinReel incrementBonusHits() function
	// currently only used by gwtw01 for it's funky bonus symbol sounds 
	public virtual bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return false;
	}

	public virtual IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		// This function doesn't block the spin from ending. Be wary of using it.
		yield break;
	}

	// ExecuteOnSymbolAnimationFinished section
	// function called from SymbolAnimator.cs when a symbol has stopped playing it's animation.
	// If you're looping through paylines this can get called multiple times on the same symbol per outcome.
	public virtual bool needsToExecuteOnSymbolAnimationFinished(SlotSymbol symbol)
	{
		return false;
	}

	public virtual IEnumerator executeOnSymbolAnimationFinished(SlotSymbol symbol)
	{
		yield break;
	}

	// executeForSymbolAnticipation() section
	// function called from SpinReel.cs when a symbol is going to play an anticipation, if this module triggers there, it will cancel the default functionality
	public virtual bool needsToExecuteForSymbolAnticipation(SlotSymbol symbol)
	{
		return false;
	}

	public virtual void executeForSymbolAnticipation(SlotSymbol symbol)
	{}

	// onBaseGameLoad() section
	// functions here are called when the base game is loading and won't close the load screen until they are finished.
	public virtual bool needsToExecuteOnBaseGameLoad(JSON slotGameStartedData)
	{
		return false;
	}

	public virtual IEnumerator executeOnBaseGameLoad(JSON slotGameStartedData)
	{
		yield break;
	}
	
	// setReelStopOrder() section
	// used in SetReelStopOrder module, called from ReelGame.setReelStopOrder()
	// provides visual editing in unity when setting up reel stop order for normal, independent, layered and hybrid reel games. 
	public virtual bool needsToSetReelStopOrder()
	{
		return false;
	}

	public virtual ReelGame.StopInfo[][] setReelStopOrder()
	{
		return null;
	}

	// executeChangeSymbolLayerAfterSymbolAnimation() section
	// Called from SymbolAnimator.stopAnimation() when a symbol that was animating is stopped and reset and you want to change the symbols layer
	// Note: This hook is implied to change the symbol's layer, and as such is currently ignored in Layered and Independent Reel
	// games, since changing layers in those games could break how the symbol is displayed
	public virtual bool needsToExecuteChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{
		return false;
	}
	
	public virtual void executeChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{}
	
	// not sure where to put this
	public virtual bool shouldIgnoreMagicReelStopTiming()
	{
		return false;
	}
	
	public virtual bool needsToExecuteRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return false;
	}
	
	public virtual string executeRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return "";
	}
	
	public virtual bool needsToExecuteRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return false;
	}
	
	public virtual string executeRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return "";
	}
	
	public virtual bool needsToExecuteRollupSoundLengthOverride()
	{
		return false;
	}
	
	public virtual float executeRollupSoundLengthOverride(string soundKey)
	{
	    // Return -1 because that's the default value if this function wasn't being used.
	    Debug.LogWarning("Calling the base function of executeRollupSoundLengthOverride, if the defualt value is desired needsToExecuteRollupSoundLengthOverride should return false.");
	    return -1.0f;
	}

	public virtual float executeRollupSoundLengthOverride(long payout)
	{
	    // Return -1 because that's the default value if this function wasn't being used.
	    Debug.LogWarning("Calling the base function of executeRollupSoundLengthOverride, if the defualt value is desired needsToExecuteRollupSoundLengthOverride should return false.");
	    return -1.0f;
	}
	
// executeOverridePaylineSounds(string symbolName)
// allow payline sound to be overridded
	public virtual bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		return false;
	}

	public virtual void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{}

	// For example, in wicked01 Free Spins,
	// if you get a 2x wild on the pay line,
	// play a sound effect every time it animates the 2x wild symbol
	
	public virtual bool needsToPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		return false;
	}

	public virtual void executePlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
	}
	
	public virtual bool needsToExecutePreShowNonBonusOutcomes()
	{
		return false;
	}

	public virtual void executePreShowNonBonusOutcomes()
	{}
	
	public virtual bool needsToGetCarryoverWinnings()
	{
		return false;
	}
	
	public virtual long executeGetCarryoverWinnings()
	{
		return 0;
	}
	
	public virtual bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return false;
	}
	
	public virtual IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		yield break;
	}

	public virtual bool needsToExecuteAfterLoadingScreenHidden()
	{
		return false;
	}

	public virtual IEnumerator executeAfterLoadingScreenHidden()
	{
		yield break;
	}

	public virtual bool needsToExecuteOnWagerChange(long currentWager)
	{
		return false;
	}

	public virtual void executeOnWagerChange(long currentWager)
	{

	}

	// needsToExecuteDuringContinueWhenReady() section
	// called from continueWhenReady() after all wins are paid out but before the game is unlocked
	// currently used by munsters01 to trigger the tug of war picking game after all wins are rolled up
	public virtual bool needsToExecuteDuringContinueWhenReady()
	{
		return false;
	}

	public virtual IEnumerator executeDuringContinueWhenReady()
	{
		yield break;
	}

	// needsToTriggerBigWinBeforeSpinEnd() section
	// allows the big win to handled by the slot module, by returning true from isModuleHandlingBigWin
	// the big win will then be custom triggered by the module when executeTriggerBigWinBeforeSpinEnd is called from continueWhenReady
	public virtual bool isModuleHandlingBigWin()
	{
		// controls if the big win should be delayed
		// NOTE: This needs to return false at some point after return true once a module determines the big win can occur, otherwise big wins will not trigger
		return false;
	}

	public virtual bool needsToTriggerBigWinBeforeSpinEnd()
	{
		return false;
	}

	public virtual IEnumerator executeTriggerBigWinBeforeSpinEnd()
	{
		// Setup and trigger the big win the way you want it to (rather than the way it would normally trigger)
		yield break;
	}

	// tells if this game will handle launching the Big Win End dialogs itself due to not wanting them to trigger right after the big win ends
	// if you make this true then in your module you should call SlotBaseGame.showBigWinEndDialogs
	// see munsters01 TugOfWarModule for an example 
	public virtual bool willHandleBigWinEndDialogs()
	{
		return false;
	}

	// isCurrentlySpinBlocking() section
	// tells if a module is currenlty blocking a spin from continuing
	public virtual bool isCurrentlySpinBlocking()
	{
		// controls if the spin will currently be blocked by this module
		// NOTE: In your override this will need to return false at some point if it returns true so that the spins will unlock correctly
		return false;
	}

	//Executes during the bonus symbol animation phase just before going into freespin mode
	public virtual bool needsToExecuteOnPlayBonusSymbolsAnimation()
	{
		return false;
	}

	public virtual IEnumerator executeOnPlayBonusSymbolsAnimation()
	{
		yield break;
	}

	// executeOnBonusGameCreated() section
	// used to handle delays transitions after the bonus game is created for transition effects that require the bonus to be already instantiated
	public virtual bool needsToExecuteOnBonusGameCreatedSync()
	{
		Debug.LogError("This game from the TV sync and hasn't been hooked up properly yet");
		return false;
	}

	public virtual IEnumerator executeOnBonusGameCreatedSync()
	{
		Debug.LogError("This game from the TV sync and hasn't been hooked up properly yet");
		yield break;
	}

	public virtual bool needsToExecuteOnBonusGameEndedSync()
	{
		Debug.LogError("This game from the TV sync and hasn't been hooked up properly yet");
		return false;
	}

	public virtual IEnumerator executeOnBonusGameEndedSync()
	{
		Debug.LogError("This game from the TV sync and hasn't been hooked up properly yet");
		yield break;
	}

	public virtual bool needsToExecuteOnReevaluationSpinStartSync()
	{
		return false;
	}

	public virtual IEnumerator executeOnReevaluationSpinStartSync()
	{
		yield break;
	}

	// executed when we finished a reevaluation spin
	public virtual bool needsToExecuteOnReevaluationSpinEnd()
	{
		return false;
	}

	public virtual void executeOnReevaluationSpinEnd()
	{
		
	}

	public virtual bool needsToExecuteOnSpecificReelNudging(SlotReel reel)
	{
		return false;
	}
	
	public virtual IEnumerator executeOnSpecificReelNudging(SlotReel reel)
	{
		yield break;
	}

	public virtual bool needsToExecuteOnSymbolPositionChanged()
	{
		return false;
	}

	public virtual void OnSymbolPositionChanged(float symbolNormalizedPosY, int reelIndex, SymbolAnimator symbolAnimator)
	{
		return;
	}

	// executeOnContinueToBasegameFreespins() section
	// functions in this section are executed when SlotBaseGame.continueToBasegameFreespins() is called to start freespins in base
	// NOTE: These modules will trigger right at the start of the transition to freespins in base, before the spin panel is changed and the game is fully ready to start freespining
	public virtual bool needsToExecuteOnContinueToBasegameFreespins()
	{
		return false;
	}

	public virtual IEnumerator executeOnContinueToBasegameFreespins()
	{
		yield break;
	}
	
	// executeOnReturnToBasegameFreespins is the inverse of executeOnContinueToBaseGameFreespins()
	// these will trigger right at the start of the transition from freespins back to base, before spin panel transitions and any big win starts
	public virtual bool needsToExecuteOnReturnToBasegameFreespins()
	{
		return false;
	}

	public virtual IEnumerator executeOnReturnToBasegameFreespins()
	{
		yield break;
	}
	
	// Executed before the created Free Spins prefab has been shown
	public virtual bool needsToExecuteBeforeFreeSpinsIsShown()
	{
		return false;
	}

	public virtual IEnumerator executeBeforeFreeSpinsIsShown()
	{
		yield break;
	}

		public virtual bool needsToExecuteOnSymbolAnimatorsOnOff()
	{
		return false;
	}

	public virtual IEnumerator executeOnSymbolAnimatorsOnOff(SymbolAnimator symbolAnimator, bool shouldPlay, string animationState)
	{
		yield break;
	}

	// Executed in TumbleReel.updateSymbolPositions(), used to check if a module is going to perform the symbol cleanup step
	// if a module doesn't then the TumbleReel will perform the cleanup
	public virtual bool isCleaningUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return false;
	}

	// Executed in TumbleReel.updateSymbolPositions(), used to perform an action before a symbol is being cleaned up because
	// it was part of a win in the tumble reel and will be removed, for instance the poof effects which are handled by the
	// HideTumbleSymbolsModule.  If your module will also cleanup the symbol then you should also override
	// isCleaningUpWinningSymbolInTumbleReel (see above)
	public virtual bool needsToExecuteBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return false;
	}

	public virtual IEnumerator executeBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		yield break;
	}

	// Executed in TumbleReel
	public virtual bool needsToTumbleSymbolFromModule()
	{
		return false;
	}

	public virtual IEnumerator tumbleSymbolFromModule(SlotSymbol symbol, float offset)
	{
		yield break;
	}

	// Executed in TumbleReel
	public virtual bool needsToChangeTumbleReelWaitFromModule()
	{
		return false;
	}

	public virtual IEnumerator changeTumbleReelWaitFromModule(int reelId)
	{
		yield break;
	}

	// Function that executes once all symbols are setup to tumble in, used to mimic certain symbol anticipations in tumble games
	// where certain reel concepts don't exist/or are a bit different, first used in spam01 for the wild multiplier sounds
	public virtual bool needsToExecuteOnTumbleReelSymbolsTumbling(TumbleReel reel)
	{
		return false;
	}

	public virtual IEnumerator executeOnTumbleReelSymbolsTumbling(TumbleReel reel)
	{
		yield break;
	}

	// Function for telling if a module has covered a symbol location, some modules may rely on this info to determine what to do
	// to query this for all modules use ReelGame.isSymbolLocationCovered()
	public virtual bool isSymbolLocationCovered(SlotReel reel, int symbolIndex)
	{
		return false;
	}

	// Executed via BonusGamePresenter before it call finalCleanup to actually finish and destroy a bonus
	// allows for stuff like playing transition animations after the bonus game is over and all dialogs are closed
	// but before the bonus game is destroyed
	public virtual bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return false;
	}

	public virtual IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		yield break;
	}

	// Control if the reel game wants the Overlay and SpinPanel turned back on when returning from a bonus
	// you may want to skip that step if for instance eyou have a transition that will do it for you
	// NOTE: Must be attached to SlotBaseGame
	public virtual bool isEnablingOverlayWhenBonusGameEnds()
	{
		return true;
	}

	public virtual bool isEnablingSpinPanelWhenBonusGameEnds()
	{
		return true;
	}

	//function for telling that a module is using custom reel height info when mutating reel heights
	public virtual bool shouldUseCustomReelSetData()
	{
		return false;
	}

	public virtual Dictionary<int, ReelData> getCustomReelSetData()
	{
		return null;
	}

	// needsToOverrideLegacyPlopTumbleWaitTimePerCluster() section
	// special module hook made to allow altering this value from the default in
	// legacy plop and tumble games in order to alter the timing of those games
	public virtual bool needsToOverrideLegacyPlopTumbleWaitTimePerCluster()
	{
		return false;
	}

	public virtual float getLegacyPlopTumbleWaitTimePerClusterOverride()
	{
		return 0.0f;
	}


	// executeOnSlotEngineFrameUpdateAdvancedSymbols() section
	// Module hook for handling stuff after the reels have advanced in SlotEngine in SlotEngine.frameUpdate()
	// Note: there is no real concept of blocking in that area, so anything you want to do here must be accomplished
	// in a single frame or handled in some non blocking coroutine that you spawn
	public virtual bool needsToExecuteOnSlotEngineFrameUpdateAdvancedSymbols()
	{
		return false;
	}

	public virtual void executeOnSlotEngineFrameUpdateAdvancedSymbols()
	{}

	// executeSetSymbolAnimatorStopPoint() section
	// Module that hooks into SymbolAnimator.turnOffAnimators for the purpose of controlling what 
	// point the animator goes to when stopped, useful if you want the animator to stop at the beginning
	// until it has been played, and then stop at the end
	public virtual bool needsToSetSymbolAnimatorStopPoint(SlotSymbol symbol)
	{
		return false;
	}

	public virtual float executeSetSymbolAnimatorStopPoint(SlotSymbol symbol)
	{
		return 0.0f;
	}

	// executeOnSymbolAnimatorPlayed() section
	// Module hook for handling something when SymbolAnimator.playAnimation has been called
	// can be useful if you want to track when symbols are animated.
	public virtual bool needsToExecuteOnSymbolAnimatorPlayed(SlotSymbol symbol)
	{
		return false;
	}

	public virtual void executeOnSymbolAnimatorPlayed(SlotSymbol symbol)
	{}

	// executeGetClobberSymbolReplacementListOverride() section
	// Functions here control an override which can happen for each reel to control
	// what are valid clobber symbols for that reel (instead of using the default
	// which is to just use any 1x1's on that reel)
	public virtual bool needsToExecuteGetClobberSymbolReplacementListOverride(SlotReel reel)
	{
		return false;
	}

	public virtual List<string> executeGetClobberSymbolReplacementListOverride(SlotReel reel)
	{
		return null;
	}

	// executeOnClearOutcomeDisplay() section
	// Hook for when ReelGame.clearOutcomeDisplay is called. Note that this module
	// hook is not a coroutine since it isn't really safe in all
	// cases to have this cause the game to block.
	// Ideally this hook should only be used for basic cleanup
	// that needs to happen at the same time that the outcome display
	// is cleaned.
	public virtual bool needsToExecuteOnClearOutcomeDisplay()
	{ 
		return false;
	}

	public virtual void executeOnClearOutcomeDisplay()
	{}
	
	// isBlockingWebGLKeyboardInputForSlotGame() section
	// Used to block the game from accepting WebGL game input
	// for instance if the game has a psuedo dialog like elvis03
	// where we have a bet selector that goes over the game and
	// should prevent interaction with the game behind it, this
	// way the player can't use the space bar to spin or arrow keys
	// to change bet amount while this dialog is up
	public virtual bool isBlockingWebGLKeyboardInputForSlotGame()
	{
		return false;
	}

	// isModulePreventingBaseGameAudioFade() section
	// Used to prevent audio from fading out after a set amount of time
	// can be used for stuff like elvis03 bet selector where sound should
	// not fade until the player has picked a bet and is actually into the 
	// base game
	public virtual bool isModulePreventingBaseGameAudioFade()
	{
		return false;
	}
	
	// needsToOverridePaytableSymbolName() section
	// Used for games to override what symbol prefab gets loaded for a given symbol
	// for the paytable.  Useful for games like billions01 that have two versions of
	// majors, or games where you want to swap to another prefab for some reason.
	public virtual bool needsToOverridePaytableSymbolName(string name)
	{
		return false;
	}

	public virtual string getOverridePaytableSymbolName(string name)
	{
		return name;
	}
	
	// getOverridenBonusOutcome() section
	// Used for games like got01 where the bonus outcome needs to be acquired
	// and used in a slightly different way than normal (since in got01 the
	// feature is what actually triggers the bonus)
	public virtual bool needsToUseOverridenBonusOutcome()
	{
		return false;
	}

	public virtual SlotOutcome getOverridenBonusOutcome()
	{
		return null;
	}
	
	// isPayingBasegameWinsBeforeFreespinsInBase() section
	// By setting this you can control if basegame wins are fully paid out
	// before going to Freespin in Base.  This only has any affect on games that use
	// freespin in base.
	//
	// Normally basegame wins are simply carried over to the freespin in base and
	// everything is paid out at the end.  If there is a reason why you wouldn't want
	// the value carried over, like there is a multiplier that is only applied to
	// freespins wins, you can use this in order to payout the value before freespins.
	//
	// NOTE: If any module sets this to true it will be treated as being used
	// NOTE: Big wins will not occur for the base winnings even if they go over the
	// threshold (since it would be a fairly big interruption for transitioning into
	// freespins).  This also means that the final bonus payout will only big win
	// if it goes over the big win threshold on its own.
	public virtual bool isPayingBasegameWinsBeforeFreespinsInBase()
	{
		return false;
	}

	// isPayingBasegameWinsBeforeBonusGames() section
	// By setting this you can control if basegame wins are fully paid out
	// before going into bonus games (including delaying their transitions until
	// after base game payout).
	//
	// Normally basegame wins are paid out after bonus games, but for gen97 Cash Tower and maybe
	// some future games we want to pay out the base game wins before the bonus game
	// due to the bonus functioning in a way where we want to get the base payouts done
	// before starting the complciated bonus game flow.
	//
	// NOTE: If any module sets this to true it will be treated as being used
	// NOTE: Big wins will not occur for the base winnings even if they go over the
	// threshold (since it would be a fairly big interruption for transitioning into
	// bonuses).  This also means that the final bonus payout will only big win
	// if it goes over the big win threshold on its own.
	public virtual bool isPayingBasegameWinsBeforeBonusGames()
	{
		return false;
	}

	// isHandlingSlotReelClearSymbolOverridesWithModule() section
	// If this returns true, the ReelGame will *NOT* automatically clear all of the
	// SlotReel.symbolOverrides after a spin, and it will be up to a module to do
	// the clearing when it thinks is appropriate.
	public virtual bool isHandlingSlotReelClearSymbolOverridesWithModule()
	{
		return false;
	}
}
