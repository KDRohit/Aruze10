using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Created by Stephen Arredondo
 * First used in Batman01
 * Module to handle playing two reel anticipations at the same time.
 * Anticipations in this game are also triggered differently than traditional gmaes. 
 * The anticipations start once we see BN symbols spinning on the reels.
*/
public class SimultaneousReelAnticipationModule : SlotModule
{
	[SerializeField] private GameObject foregroundReels;
	[SerializeField] private GameObject secondaryAnticipation;
	[SerializeField] private int reelToAnticipateOn = 0; //raw reel ID (0-indexed)
	[SerializeField] private int layerToAnticipateOn = 0;
	[SerializeField] private int secondLayerToAnticipateOn = 0;
	[SerializeField] private string[] reelsetsThatContainsBonuses; // Names of the reelsets that contain BN reel strips
	[SerializeField] private float anticipationLength = 0.0f;
	[SerializeField] private float anticipationSoundDelay = 0.0f;
	private bool isAnticipating = false;

	private const string leftGameAnticipateSound = "bonus_mega_anticipate";
	private const string rightGameAnticipateSound = "bonus_mega_anticipate_game1";

	public override bool needsToPlayReelAnticipationEffectFromModule (SlotReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		isAnticipating = false;
		yield return null;
	}

	public override bool needsToExecuteOnReelsStoppedCallback ()
	{
		return isAnticipating;
	}

	public override bool needsToHideReelAnticipationEffectFromModule (SpinReel stoppedReel)
	{
		return isAnticipating;
	}

	public override IEnumerator hideReelAnticipationEffectFromModule (SpinReel stoppedReel)
	{
		bool hideSecondAnticipation = true;
		if (stoppedReel.layer == layerToAnticipateOn && (reelToAnticipateOn + 1) == stoppedReel.reelID) //Want to decide what to hide/keep showing once our first anticipating reel has landed
		{
			string[] finalSymbols = reelGame.engine.getSlotReelAt(reelToAnticipateOn, -1, layerToAnticipateOn).getFinalReelStopsSymbolNames();

			for (int i = 0; i < finalSymbols.Length; i++)
			{
				if (SlotSymbol.isBonusSymbolFromName(finalSymbols[i]))
				{
					hideSecondAnticipation = false;
					break;
				}
			}
			reelGame.engine.hideAnticipationEffect(reelToAnticipateOn + 1); //We want to hide this one once these reels land. 
			if (hideSecondAnticipation)
			{
				secondaryAnticipation.SetActive(false); //Only hide if the other layer didn't have any bonuses
			}
			else
			{
				StartCoroutine(playSecondSoundAfterDelay()); //Need to delay the 2nd anticipation so it isn't aborted by the first BN symbol fanfare
			}
		}
		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop (SlotReel stoppedReel)
	{
		return stoppedReel.layer == secondLayerToAnticipateOn && stoppedReel.getRawReelID() == reelToAnticipateOn;
	}

	public override IEnumerator executeOnSpecificReelStop (SlotReel stoppedReel)
	{
		secondaryAnticipation.SetActive(false);
		yield return null;
	}

	private bool isReelsetWithBnSymbols()
	{
		LayeredSlotEngine engine = reelGame.engine as LayeredSlotEngine;
		string leftFGReelset = engine.reelLayers[layerToAnticipateOn].reelSetData.keyName;

		for (int i = 0; i < reelsetsThatContainsBonuses.Length; i++)
		{
			if (leftFGReelset == reelsetsThatContainsBonuses[i])
			{
				return true;
			}
		}

		return false;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		LayeredSlotEngine engine = reelGame.engine as LayeredSlotEngine;
		string leftFGReelset = engine.reelLayers[layerToAnticipateOn].reelSetData.keyName;

		bool hasBonusSymbolsInReelset = isReelsetWithBnSymbols();

		foregroundReels.gameObject.SetActive(hasBonusSymbolsInReelset);
		return hasBonusSymbolsInReelset;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		isAnticipating = true;
		reelGame.engine.playAnticipationEffect(reelToAnticipateOn, -1, -1, layerToAnticipateOn);
		secondaryAnticipation.SetActive(true);
		Audio.play(Audio.soundMap(leftGameAnticipateSound));
		yield return new TIWaitForSeconds(anticipationLength);
	}

	private IEnumerator playSecondSoundAfterDelay()
	{
		yield return new TIWaitForSeconds(anticipationSoundDelay);
		Audio.play(Audio.soundMap(rightGameAnticipateSound)); //Start playing our 2nd anticipation sound if the first BN landed on the reels
	}
}
