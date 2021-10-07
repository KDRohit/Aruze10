using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// This module checks for linked reels data in the freespins outcome and plays an ambient effect (e.g. lighting strike),
//	Resets the linked reels to the begining, and optionaly scrambles unlinked reels.
//	Useful for games in the style of elvira01
public class RandomizingLinkedReelsModule : SlotModule 
{
	[SerializeField] private Animator[] linkedReelFrames;
	[SerializeField] private GameObject[] reelMarkers;
	[SerializeField] private GameObject ambientEffect;
	[SerializeField] private float ambientEffectAnimTime = 0.0f;
	[SerializeField] private float frameEffectAnimTime = 0.0f;
	[SerializeField] private string frame2xAnimName = "";
	[SerializeField] private string frame3xAnimName = "";
	[SerializeField] private bool shouldAnimateOutcomeOnPrespin = true;
	[SerializeField] private bool shouldRandomizeUnlinkedReels = true;
	[SerializeField] private GameObject[] objectsToActivateOnFirstSpin;

	private bool objectsRevealed = false;
	private int[] linkedReelsData;
	private Animator visibleReelFrame;

	private const string LINKED_REEL_REVEAL_SOUND_KEY = "linked_reel_reveal";

	public override bool needsToExecuteOnPreSpin()
	{
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.peekNextOutcome() != null)
		{
			SlotOutcome nextOutcome = FreeSpinGame.instance.peekNextOutcome();
			if (nextOutcome != null)
			{
				linkedReelsData = nextOutcome.getOutcomeJsonValue<int[]>(JSON.getIntArrayStatic, "linked_reels");
				if (linkedReelsData != null && linkedReelsData.Length > 0)
				{
					return true;;
				}
			}
		}
		return false;
	}

	private IEnumerator playAmbientEffect()
	{
		ambientEffect.SetActive(true);
		yield return new TIWaitForSeconds(ambientEffectAnimTime);
		ambientEffect.SetActive(false);
	}

	public override IEnumerator executeOnPreSpin()
	{
		// Reveal any objects we need to if this is the first spin
		if (!objectsRevealed)
		{
			foreach (GameObject obj in objectsToActivateOnFirstSpin)
			{
				obj.SetActive(true);
			}
			objectsRevealed = true;
		}

		int firstReelChanged = -1;
		int numLinkedReelsChanged = 0;

		if (visibleReelFrame != null)
		{
			visibleReelFrame.gameObject.SetActive(false);
		}

		if (linkedReelFrames != null && linkedReelFrames.Length > linkedReelsData.Length)
		{
			visibleReelFrame = linkedReelFrames[linkedReelsData.Length];
			if (visibleReelFrame != null)
			{
				Vector3 markerPos = reelMarkers[linkedReelsData[0]].transform.position;
				Vector3 reelFramePos = visibleReelFrame.transform.position;
				visibleReelFrame.transform.position = new Vector3(markerPos.x, markerPos.y, reelFramePos.z);
				visibleReelFrame.transform.localScale = reelMarkers[linkedReelsData[0]].transform.localScale;
				Audio.tryToPlaySoundMap(LINKED_REEL_REVEAL_SOUND_KEY);
				visibleReelFrame.gameObject.SetActive(true);

				//Refresh all symbols on each reel using the new reel strips before we spin again
				Dictionary<int, string> reelStrips = FreeSpinGame.instance.peekNextOutcome().getReelStrips();

				// This will kill the paylines
				ReelGame.activeGame.outcomeDisplayController.clearOutcome();

				SlotReel[] reelArray = reelGame.engine.getReelArray();

				// First go through the reels and fix anything that is going to break by 
				// swapping in the new reel strips
				for (int i = 0; i < reelArray.Length; i++)
				{
					SlotReel reel = reelArray[i];

					if (reelStrips.Keys.Contains(reel.reelID))
					{
						// Make sure that if we are dealing with the first or last linked reel
						// we split any straddled mega symbols so that they aren't broken by the reel
						// symbol change we are going to do below.
						if (!reelStrips.ContainsKey(reel.reelID - 1))
						{
							SlotReel previousReel = reelGame.engine.getSlotReelAt((reel.reelID - 1) - 1);
							if (previousReel != null)
							{
								List<SlotSymbol> symbolList = previousReel.symbolList;
								for (int k = 0; k < symbolList.Count; k++)
								{
									SlotSymbol currentSymbol = symbolList[k];
									int symbolWidth = (int)currentSymbol.getWidthAndHeightOfSymbol().x;
									if (currentSymbol.getColumn() != symbolWidth)
									{
										currentSymbol.splitSymbol();
									}
								}
							}
						}
						else if (!reelStrips.ContainsKey(reel.reelID + 1))
						{
							SlotReel nextReel = reelGame.engine.getSlotReelAt((reel.reelID - 1) + 1);
							if (nextReel != null)
							{
								List<SlotSymbol> symbolList = nextReel.symbolList;
								for (int k = 0; k < symbolList.Count; k++)
								{
									SlotSymbol currentSymbol = symbolList[k];
									if (currentSymbol.getColumn() != 1)
									{
										currentSymbol.splitSymbol();
									}
								}
							}
						}

						if (firstReelChanged == -1)
						{
							firstReelChanged = reel.reelID;
						}
						numLinkedReelsChanged++;
					}
				}

				// Now that we've corrected any issues the reels would have let us actually do the swap
				// We don't want to do the swap in the code above since that could break mega symbols
				// and cause them to not be split correctly
				for (int i = 0; i < reelArray.Length; i++)
				{
					SlotReel reel = reelArray[i];

					if (reelStrips.Keys.Contains(reel.reelID))
					{
						ReelStrip reelStrip = ReelStrip.find(reelStrips[reel.reelID]);
						reel.setSymbolsToReelStripIndex(reelStrip, reelStrip.symbols.Length - 1);
					}
					else if (shouldRandomizeUnlinkedReels)
					{
						reel.setSymbolsToReelStripIndex(reel.reelData.reelStrip, UnityEngine.Random.Range(0, reel.reelData.reelStrip.symbols.Length-1));
					}
				}

				reelGame.engine.resetLinkedReelPositions();
			}
			else
			{
				Debug.LogError("The reel frame defined for linked reels in RandomizingLinkedReelsModule is null for index: " + linkedReelsData.Length);
			}
		}
		else
		{
			Debug.LogError("No reel frames defined for linked reels in RandomizingLinkedReelsModule");
		}

		// If we have a visible reel frame and we have animation keys for it, play the key
		if (visibleReelFrame != null)
		{
			if (!string.IsNullOrEmpty(frame2xAnimName) && numLinkedReelsChanged == 2)
			{
				visibleReelFrame.Play(frame2xAnimName);
			}
			if (!string.IsNullOrEmpty(frame3xAnimName) && numLinkedReelsChanged == 3)
			{
				visibleReelFrame.Play(frame3xAnimName);
			}
		}

		// Play any additional transition effects
		if (ambientEffect != null)
		{
			StartCoroutine(playAmbientEffect());
		}

		// Wait for the visible frame effect
		if (visibleReelFrame != null)
		{
			yield return new TIWaitForSeconds(frameEffectAnimTime);
		}

		// Once the megasymbols is revealed after a spin, see if we should animate it
		if (shouldAnimateOutcomeOnPrespin && numLinkedReelsChanged > 0)
		{
			SlotSymbol symbol = reelGame.engine.getVisibleSymbolsAt(firstReelChanged-1)[0];
			symbol.animateOutcome();
			// Wait for the symbol to finish animating
			if (symbol.info != null)
			{
				yield return new TIWaitForSeconds(symbol.info.customAnimationDurationOverride);
			}
		}

		yield break;
	}
}
