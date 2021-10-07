using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// If there is a sticky symbol then do some sort of expansion and modify the underlying symbols.
// @Author Leo Schnee
// NOTE!: This class is now deprecated, please use TWWildBannersModule instead, which should be cleaner and more flexible
public class DeprecatedExpandingStickySymbolModule : SlotModule 
{
	private bool[] hasTrigger;
	private bool[] isTransforming;
	[SerializeField] private Animator[] wildOverlays = new Animator[6];
	[SerializeField] private Animator[] swapOutSymbols = new Animator[6];
	[SerializeField] private string TRIGGER_SYMBOL = "TW"; // This isn't sent down in data...
	[SerializeField] private string TRANSFORM_SYMBOL = "WD"; // This isn't sent down in data...
	[SerializeField] private float TIME_EXPAND_ANIMATION = 2.0f;

	private const string TRIGGER_LAND_SOUND_KEY = "trigger_symbol";
	private const string TRIGGER_EXPAND_SOUND_KEY = "expanding_symbol";

	public override void Awake()
	{
		base.Awake();
		isTransforming = new bool[wildOverlays.Length];
		hasTrigger = new bool[wildOverlays.Length];
	}

	public override bool needsToExecuteOnPreSpin()
	{
		foreach (bool shouldExecute in hasTrigger)
		{
			if (shouldExecute)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnPreSpin()
	{
		// Show the overlays.
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			if (hasTrigger[reel.reelID - 1] && wildOverlays[reel.reelID - 1] != null)
			{
				if(swapOutSymbols.Length > 0)
				{
					if (swapOutSymbols[reel.reelID - 1] != null)
					{
						swapOutSymbols[reel.reelID - 1].gameObject.SetActive(true);
					}
					else
					{
						wildOverlays[reel.reelID - 1].gameObject.SetActive(true);
					}
				}
			}
		}
		yield break;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel reel)
	{
		return hasTrigger[reel.reelID - 1];
	}
	
	public override void executeOnSpecificReelStopping(SlotReel reel)
	{
		// If the reels already covered up it shouldn't play a stop sound.
		reel.shouldPlayReelStopSound = false;
	}


// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel reel)
	{
		return !hasTrigger[reel.reelID - 1];
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel reel)
	{
		// We want to play the anticipationAnimaton when a reel stops with a TRIGGER_SYMBOL, becuase we are about to expand it.
		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			if (symbol.name == TRIGGER_SYMBOL)
			{
				symbol.animateAnticipation();
				Audio.play(Audio.soundMap(TRIGGER_LAND_SOUND_KEY));
				break;
			}
		}
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(playTriggerAnimations());
		yield return StartCoroutine(expandAllTriggerSymbols());
	}

	private IEnumerator expandAllTriggerSymbols()
	{
		bool playedOnce = false; // Keep track of this so we don't double play the sound.
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			// Do all of the TW stuff.
			if (!hasTrigger[reel.reelID - 1])
			{
				foreach (SlotSymbol symbol in reel.visibleSymbols)
				{
					if (symbol.name == TRIGGER_SYMBOL)
					{
						hasTrigger[reel.reelID - 1] = true;
						StartCoroutine(expandTriggerOn(reel, !playedOnce));
						playedOnce = true;
						break;
					}
				}
			}
		}

		bool shouldwait = true;
		while (shouldwait)
		{
			shouldwait = false;
			foreach (bool reelTransforming in isTransforming)
			{
				if (reelTransforming)
				{
					shouldwait = true;
				}
			}
			yield return null;
		}
	}
	
	private IEnumerator expandTriggerOn(SlotReel reel, bool playSound)
	{
		isTransforming[reel.reelID - 1] = true;
		wildOverlays[reel.reelID - 1].gameObject.SetActive(true);
		// Do the animation
		if (playSound)
		{
			Audio.play(Audio.soundMap(TRIGGER_EXPAND_SOUND_KEY));
		}
		// Wait for the animation to finish.
		yield return new TIWaitForSeconds(TIME_EXPAND_ANIMATION);

		// Ideally this is done with 1 animator.
		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			symbol.mutateTo(TRANSFORM_SYMBOL);
		}
		
		if (swapOutSymbols.Length > 0 && swapOutSymbols[reel.reelID - 1] != null)
		{
			swapOutSymbols[reel.reelID - 1].gameObject.SetActive(true);
			
			// Skip one frame to wait for unity to catch up before hiding the overlay
			yield return null;
			
			wildOverlays[reel.reelID - 1].gameObject.SetActive(false);
		}

		isTransforming[reel.reelID - 1] = false;
	}

	private IEnumerator playTriggerAnimations()
	{
		bool animatingTriggerSymbol = false;
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			if (!hasTrigger[reel.reelID - 1])
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1))
				{
					if (symbol.name == TRIGGER_SYMBOL)
					{
						animatingTriggerSymbol = true;
						break;
					}
				}
			}
		}
		if (animatingTriggerSymbol)
		{
			// Wait for the animation to play.
			SymbolInfo info = reelGame.findSymbolInfo(TRIGGER_SYMBOL);
			if (info != null)
			{
				yield return new TIWaitForSeconds(info.customAnimationDurationOverride);
			}
			else
			{
				Debug.LogWarning(TRIGGER_SYMBOL + " symbol is not defined.");
			}
		}
	}
}
