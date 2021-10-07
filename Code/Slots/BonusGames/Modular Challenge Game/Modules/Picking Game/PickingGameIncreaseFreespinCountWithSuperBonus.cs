using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for the picking game of gen97 Cash Tower that goes before the freespins where you reveal how many spins you will get,
 * with a bad reveal which will end the picking game and proceed into the freespins.  This class extends PickingGameIncreaseFreespinCountWithSuperBonus
 * in order to handle a Super Bonus meter which can launch a Super Freespins which can be played at any time in the picking game up
 * until the gameover reveal occurs.  Basically it could occur right when you enter from trigger symbols adding to the bar, or when revealing
 * a non-gameover pick which will also add to the super bar.
 *
 * Creation Date: 2/4/2020
 * Original Author: Scott Lepthien
 */
public class PickingGameIncreaseFreespinCountWithSuperBonus : PickingGameIncreaseFreespinCountModule
{
	[Header("Super Bonus Settings")]
	[Tooltip("The label that will display the super bonus value which will award when the freespins is over.")]
	[SerializeField] private LabelWrapperComponent superBonusValueText;
	[Tooltip("Object that is moved in order to show how filled in the Super Bonus bar is.")] 
	[SerializeField] private GameObject superBonusBarMover;
	[Tooltip("Point for the bar mover where the bar appears unfilled.  Will interpolate between this and barFilledPoint to display how filled in the bar is.")]
	[SerializeField] private Vector3 barUnfilledPoint;
	[Tooltip("Point for the bar mover where the bar appears fully filled.  Will interpolate between this and barUnfilledPoint to display how filled in the bar is.")]
	[SerializeField] private Vector3 barFilledPoint;
	[Tooltip("Point for the bar mover where the bar appears fully filled.  Will interpolate between this and barUnfilledPoint to display how filled in the bar is.")]
	[SerializeField] private AnimationListController.AnimationInformationList displaySuperBonusBarValueAnims;
	[Tooltip("Particle effect played when the game first starts to award the SC symbol landed amount (already in spin count) towards the Super Bonus bar")]
	[SerializeField] private AnimatedParticleEffect startValueToSuperBarParticleEffect;
	[Tooltip("Delay before the super bonus is launched, ensuring that players can see the bar fill.  May need less if some extra animation is added before the Super Bonus occurs.")]
	[SerializeField] private float delayBeforeLaunchingSuperBonus = 1.0f;
	[Tooltip("Animations played prior to the game transitioning to the Super Bonus")]
	[SerializeField] private AnimationListController.AnimationInformationList superBonusTransitionAnims; 
	[Tooltip("Particle effect played when a value pick is revealed that goes from the pick to the Super Bonus bar")]
	[SerializeField] private AnimatedParticleEffect pickRevealValueToSuperBarParticleEffect;
	[Tooltip("Animations to play on the meter everytime it moves ")] 
	[SerializeField] private AnimationListController.AnimationInformationList moveMeterAnims;
	
	protected int currentSuperBarFillAmount = 0;		// What amount the Super Bonus bar is already filled.  This will be increased via a bonus game, but will be tracked in this class.

	private JSON superBonusMeterJson;
	private StickAndWinWithBlackoutPickModule baseGameSuperBonusModule;
	private SlotOutcome superBonusOutcome;
	private int superBonusMeterSizeOverride = 0;

	private const string SUPER_BONUS_METER_JSON_KEY = "super_bonus_meter";
	private const string SUPER_BONUS_METER_SIZE_JSON_KEY = "super_bonus_meter_size"; // Override used for some cheats that changes the length of the bar to make it easier to trigger the Super Bonus
	private const string FREESPIN_METER_REEVAL_TYPE = "cash_tower"; // @todo : Consider making this settable so that another game using a different reevaluator but similar data could reuse this class
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		
		// Get the StickAndWinWithBlackoutPickModule from the BaseGame since that has a SuperBonus meter that needs to track
		// the changes that happen in this bonus
		foreach (SlotModule module in SlotBaseGame.instance.cachedAttachedSlotModules)
		{
			StickAndWinWithBlackoutPickModule convertedModule = module as StickAndWinWithBlackoutPickModule;
			if (convertedModule != null)
			{
				baseGameSuperBonusModule = convertedModule;
				break;
			}
		}
		
		// Extract out the Super Bonus info for how much bar was filled from the symbols that triggered the bonus.
		JSON[] reevaluations = SlotBaseGame.instance.outcome.getArrayReevaluations();
		foreach (JSON reevalJson in reevaluations)
		{
			string reevalType = reevalJson.getString("type", "");
			if (reevalType == FREESPIN_METER_REEVAL_TYPE)
			{
				superBonusMeterJson = reevalJson.getJSON(SUPER_BONUS_METER_JSON_KEY);
				
				// Check for a super bonus meter size override which can come down during cheats
				// to alter the size of the bar temporarily until the Super Bonus is triggered
				superBonusMeterSizeOverride = reevalJson.getInt(SUPER_BONUS_METER_SIZE_JSON_KEY, 0);
			}
		}
		
		if (superBonusMeterJson == null)
		{
			Debug.LogError("PickingGameIncreaseFreespinCountModule.executeOnRoundInit() - Unable to find: " + SUPER_BONUS_METER_JSON_KEY + "; field in reevalType: " + FREESPIN_METER_REEVAL_TYPE);
		}
		
		// init the starting value of the super bonus meter
		currentSuperBarFillAmount = superBonusMeterJson.getInt("old", 0);
		
		// Do a verification that the base game value for the bar matches the value we are starting this bonus with
		baseGameSuperBonusModule.verifySuperBonusBarFillAmountMatches(currentSuperBarFillAmount);
		
		// make sure to cap it with whatever the current bar length is (might be overriden via cheats)
		capSuperBarFillAmount();

		updateSuperBonusBarFill();
		
		// Grab the Super Bonus outcome if it exists.  It will always be in the same spot, and we have to trigger it when
		// the Super Bonus meter becomes full
		superBonusOutcome = round.getCurrentRoundOutcome().specialBonusOutcome;
	}
	
	// executeOnRoundStarted() section
	// executes right when a round starts or finishes initing.
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		if (superBonusMeterJson != null)
		{
			int addSuperBonusBarFill = superBonusMeterJson.getInt("delta", 0);

			// Play a particle effect here before awarding the value
			if (startValueToSuperBarParticleEffect != null)
			{
				yield return StartCoroutine(startValueToSuperBarParticleEffect.animateParticleEffect());
			}
			
			yield return StartCoroutine(incrementSuperBarFillAmount(addSuperBonusBarFill));
		}
	}
	
	protected override IEnumerator revealSpinCountPick(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPick)
	{
		yield return StartCoroutine(base.revealSpinCountPick(pickItem, currentPick));

		// Play a particle trail from the pick to the Super Bonus meter
		if (pickRevealValueToSuperBarParticleEffect != null)
		{
			yield return StartCoroutine(pickRevealValueToSuperBarParticleEffect.animateParticleEffect(pickItem.transform));
		}
		
		yield return StartCoroutine(incrementSuperBarFillAmount(currentPick.superBonusDelta));
	}
	
	// Updates the Super Bonus bar display for how filled in it should be, and then plays the animations to display it.
	private void updateSuperBonusBarFill()
	{
		Vector3 pointDifference = barFilledPoint - barUnfilledPoint;

		int currentBarSize = getCurrentSuperBonusBarSize();
		
		float newSegmentPercent = currentSuperBarFillAmount / (float)(currentBarSize);
		superBonusBarMover.transform.localPosition = barUnfilledPoint + (pointDifference * newSegmentPercent);
	}

	// Public function so that other games that modify this value can tell this module about changes
	public IEnumerator incrementSuperBarFillAmount(int amountToAdd)
	{
		currentSuperBarFillAmount += amountToAdd;
		
		capSuperBarFillAmount();

		updateSuperBonusBarFill();
		
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(moveMeterAnims));

		baseGameSuperBonusModule.incrementSuperBarFillAmount(amountToAdd);
		
		int currentBarSize = getCurrentSuperBonusBarSize();
		if (currentSuperBarFillAmount == currentBarSize && superBonusOutcome != null)
		{
			if (delayBeforeLaunchingSuperBonus > 0.0f)
			{
				yield return new TIWaitForSeconds(delayBeforeLaunchingSuperBonus);
			}
		
			// The Super Bonus was triggered on entering this picking game, so we need to launch into it right now
			yield return RoutineRunner.instance.StartCoroutine(launchAndWaitForSuperBonus());
		}
	}

	// Get the current size of the super bonus bar, it can be overriden via cheats
	// so this function will get what the current value that should be used is
	private int getCurrentSuperBonusBarSize()
	{
		int currentBarSize = baseGameSuperBonusModule.totalBarFillAmount;
		if (superBonusMeterSizeOverride > 0)
		{
			currentBarSize = superBonusMeterSizeOverride;
		}

		return currentBarSize;
	}

	// Make sure that the currentSuperBarFillAmount doesn't exceed the current size of the bar.
	// Want to cap it to whatever the current bar length is.
	private void capSuperBarFillAmount()
	{
		int currentBarSize = getCurrentSuperBonusBarSize();
		
		if (currentSuperBarFillAmount > currentBarSize)
		{
			currentSuperBarFillAmount = currentBarSize;
		}
	}

	// Reset the Super Bonus bar fill to the starting point
	public void resetSuperBarFillAmount()
	{
		currentSuperBarFillAmount = 0;
		
		// Clear the override if we were using it, it only lasts for one Super Bonus fill
		superBonusMeterSizeOverride = 0;
		
		updateSuperBonusBarFill();
		
		baseGameSuperBonusModule.resetSuperBarFillAmount();
	}

	private IEnumerator launchAndWaitForSuperBonus()
	{
		// Play the transition anims
		if (superBonusTransitionAnims != null && superBonusTransitionAnims.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(superBonusTransitionAnims));
		}
	
		// Only does Freespins Super Bonus right now, would need an update to handle picking game bonuses
		// Need to handle multiplier override here, and then clear it when the super bonus is done (because Super bonus uses averaged bet multiplier)
		BonusGameManager.instance.betMultiplierOverride = superBonusOutcome.getBetMultiplierOverride();
		BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(superBonusOutcome);
		BonusGameManager.instance.summaryScreenGameName = superBonusOutcome.getBonusGame();
		BonusGameManager.instance.create(BonusGameType.SUPER_BONUS);
		BonusGameManager.instance.showStackedBonus(isHidingSpinPanelOnPopStack:true);

		// Make sure that the Freespins is started before we wait for it to be over
		while (FreeSpinGame.instance == null)
		{
			yield return null;
		}
		
		// Wait for the GameObject this module is attached to to become active again
		// when that happens we'll know that we can proceed to doing stuff here
		while (!pickingVariantParent.gameObject.activeInHierarchy)
		{
			yield return null;
		}

		resetSuperBarFillAmount();
		
		// We don't want the base game to try to award the super bonus amount when this
		// pick game is over and we are heading into freespins.  So we need to extract
		// the super bonus amount and store it out, then clear the amount from BonusGameManager.
		long superBonusWinAmount = BonusGameManager.instance.finalPayout;
		BonusGameManager.instance.finalPayout = 0;
		baseGameSuperBonusModule.setSuperBonusWinAmount(superBonusWinAmount);
		
		// Clear the bet multiplier override now, since the Standard Freespins will just
		// use the one from the spin that triggered this picking game
		BonusGameManager.instance.betMultiplierOverride = -1;

		// Update the display of the Super Bonus bar to show the value won
		if (superBonusValueText != null)
		{
			superBonusValueText.text = CreditsEconomy.convertCredits(superBonusWinAmount);
		}

		RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(displaySuperBonusBarValueAnims));
	}
}
