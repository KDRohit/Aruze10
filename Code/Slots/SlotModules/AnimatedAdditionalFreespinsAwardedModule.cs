//
// Populates a a label with the additional freepins won based on the difference between the defined
// amount in "free_spins" and the number passed in through "parameter" as numFreespinsOverride.
// An animation is played that shows the the player how many additional freespins they have won and
// plays a particle effect that increments the spin counter on the spin panel.
//
// note : This module relies on data that is only available in freespins and as such can not be used
// as a basegame module.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sept 29th, 2020
// Games : orig002
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedAdditionalFreespinsAwardedModule : SlotModule
{
#region serialized private member variables

	[Tooltip("Animations to play when freepins first starts to show how may addition freespins were awarded.")]
	[SerializeField] private AnimationListController.AnimationInformationList animations;

	[Tooltip("Particle effect to play from the additional freespins label to the spin count.")]
	[SerializeField] private AnimatedParticleEffect animatedParticleEffect;

	[Tooltip("Label that shows the player how many additional freespins were awarded.")]
	[SerializeField] private LabelWrapperComponent freespinLabel;

#endregion

#region private member variables

	private int freespinsAwarded;
	private List<TICoroutine> allCoroutines = new List<TICoroutine>();
	private FreeSpinsOutcome freeSpinsOutcome;

#endregion

#region slot module overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		freespinsAwarded = 0;

		if (isFreespins())
		{
			freeSpinsOutcome = getFreespinOutcome();

			if (freeSpinsOutcome != null)
			{
				freespinsAwarded = getAdditionalFreespins();
				return freespinsAwarded > 0;
			}
		}

		return false;
	}

	// Set the spin panel freespin count here so nothing blocks it when the game starts
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		setSpinPanelFreespinCountToBaseAmount();
	}

	// get the freespin outcome and check if extra freespins were awarded through numFreespinsOverride
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return isFreespins() && freespinsAwarded > 0;
	}

	// populates the additional freespins awarded, plays the animations list, and plays a particle effect to increment the
	// freespin count on the spin panel counter.
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		setSpinPanelFreespinCountToBaseAmount();
		freespinLabel.text = CommonText.formatNumber(freespinsAwarded);
		allCoroutines.Clear();
		allCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animations)));
		allCoroutines.Add(StartCoroutine(animatedParticleEffect.animateParticleEffect()));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
	}

#endregion

#region helper methods

	// Calculates the number of addional freespins awarded based on difference between the base freespins defined in
	// the game data and the numFreespinsOverride passed down in parameter.
	private int getAdditionalFreespins()
	{
		int baseNumberOfFreespinsSpins = freeSpinsOutcome.paytable.getInt("free_spins", 0);
		int numberOfFreespins = freeSpinsOutcome.numFreespinsOverride;
		return numberOfFreespins - baseNumberOfFreespinsSpins;
	}

	private void setSpinPanelFreespinCountToBaseAmount()
	{
		int baseFreespinCount = freeSpinsOutcome.paytable.getInt("free_spins", 0);
		BonusSpinPanel.instance.spinCountLabel.text = CommonText.formatNumber(baseFreespinCount);
	}

	// this is a callback the animated particle effect uses to increment the number of additional
	// freespins in the spin panel
	public void incrementSpinPanelFreespinWithAdditionalFreepins()
	{
		BonusSpinPanel.instance.spinCountLabel.text = CommonText.formatNumber(freeSpinsOutcome.numFreespinsOverride);
	}

	private FreeSpinsOutcome getFreespinOutcome()
	{
		if (BonusGameManager.instance.outcomes != null && BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING))
		{
			return (FreeSpinsOutcome) BonusGameManager.instance.outcomes[BonusGameType.GIFTING];
		}

		return null;
	}

	private bool isFreespins()
	{
		// hasFreespinGameStarted is used when freespins are played in the basegame and this should
		// allow this module to be used that way.
		return reelGame.isFreeSpinGame() || reelGame.hasFreespinGameStarted;
	}

#endregion
}

